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

    public SaveService(string fileName = CommonPath.SaveFile)
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

            // 깨진 파일 보관은 덤이다. 파일이 잠겨 복사가 실패해도 시작은 막지 않는다
            try
            {
                File.Copy(m_FilePath, m_FilePath + ".corrupt", true);
            }
            catch (Exception copyException)
            {
                CLogger.Error("SaveService", $"깨진 세이브를 .corrupt로 옮기지 못했습니다: {copyException.Message}");
            }

            return new SaveData();
        }
    }

    public void Save(SaveData data)
    {
        if (data == null)
            return;

        data.version = SaveData.CurrentVersion;

        // 백신·동기화 프로그램이 파일을 잡고 있으면 쓰기·바꿔 끼우기가 예외를 낸다. 기존 파일은 그대로 두고 다음 저장에서 다시 쓴다
        string tmpPath = m_FilePath + ".tmp";
        try
        {
            File.WriteAllText(tmpPath, JsonUtility.ToJson(data, true));

            // 있으면 한 번에 바꿔 끼운다(지우고 옮기는 사이에 꺼져도 파일이 사라지지 않게)
            if (File.Exists(m_FilePath))
                File.Replace(tmpPath, m_FilePath, null);
            else
                File.Move(tmpPath, m_FilePath);
        }
        catch (Exception e)
        {
            CLogger.Error("SaveService", $"세이브를 쓰지 못했습니다(기존 파일 유지): {e.Message}");
        }
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
        data.merchants ??= new();
        data.gearNodes ??= new();

        data.version = SaveData.CurrentVersion;

        return data;
    }
}
