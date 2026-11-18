using System;
using System.IO;
using UnityEngine;

// ==================================================

// JSON 파일 세이브. 임시 파일에 먼저 쓰고 바꿔 끼워서 저장 중 꺼져도 기존 파일이 깨지지 않게 한다.
// 읽기에 실패하면 깨진 파일을 .corrupt로 옮겨 두고 새 데이터로 시작한다
public class SaveService
{
    public string FilePath => m_FilePath;
    private readonly string m_FilePath;

    // ------------------------------------------------

    public SaveService(string fileName = "save.json")
    {
        m_FilePath = Path.Combine(Application.persistentDataPath, fileName);
    }

    // 세이브 파일 관리 ---

    public SaveData Load()
    {
        if (File.Exists(m_FilePath) == false)
            return new SaveData();

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(m_FilePath));
            return Migrate(data ?? new SaveData());
        }
        catch (Exception e)
        {
            CLogger.Error("SaveService", $"세이브를 읽지 못해 새로 시작합니다: {e.Message}");
            File.Copy(m_FilePath, m_FilePath + ".corrupt", true);
            return new SaveData();
        }
    }

    public void Save(SaveData data)
    {
        if (data == null)
            return;

        data.version = SaveData.CurrentVersion;

        string tmpPath = m_FilePath + ".tmp";
        File.WriteAllText(tmpPath, JsonUtility.ToJson(data, true));

        if (File.Exists(m_FilePath))
            File.Delete(m_FilePath);

        File.Move(tmpPath, m_FilePath);
    }

    public void Delete()
    {
        if (File.Exists(m_FilePath))
            File.Delete(m_FilePath);
    }

    private static SaveData Migrate(SaveData data)
    {
        data.items ??= new();
        data.flags ??= new();

        data.version = SaveData.CurrentVersion;

        return data;
    }
}
