using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ==================================================

// JSON 파일 세이브. 임시 파일에 먼저 쓰고 바꿔 끼워서 저장 중 꺼져도 기존 파일이 깨지지 않게 한다.
// 읽기에 실패하면 깨진 파일을 .corrupt로 옮겨 두고 새 데이터로 시작한다
public class SaveService
{
    private const string OldClearPrefix = "Region_Clear_";   // 버전 3 이전 클리어 플래그 접두사. 지금은 MapDef.ClearFlag(Clear_ID)

    // 26.11.18 명명 규칙으로 바뀐 옛 키 → 새 키(버섯 아이템 ID, 지역 클리어 플래그). 버전 3 이전 세이브에만 적용
    private static readonly Dictionary<string, string> s_DicRenamedKey = new()
    {
        { "Mushroom_Songi", "Mushroom_001_Songi" },
        { "Mushroom_Cloud", "Mushroom_002_Gureum" },
        { "Mushroom_FlyAgaric", "Mushroom_007_Gwangdae" },
        { "Region_Clear_Region_01", "Clear_Map_001_Field" },
        { "Region_Clear_Region_02", "Clear_Map_002_Ice" },
        { "Region_Clear_Region_03", "Clear_Map_003_Dino" },
    };

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

        if (data.version < 3)
            RenameKeys(data);

        data.version = SaveData.CurrentVersion;

        return data;
    }

    // 옛 키를 새 키로 바꾼다. 같은 아이템이 둘로 나뉘어 있으면 개수를 합친다
    private static void RenameKeys(SaveData data)
    {
        Dictionary<string, int> dicCount = new();
        List<string> listOrder = new();
        foreach (ItemStack stack in data.items)
        {
            string itemID = GetRenamedKey(stack.id);
            if (dicCount.ContainsKey(itemID) == false)
                listOrder.Add(itemID);

            dicCount[itemID] = (dicCount.TryGetValue(itemID, out int count) ? count : 0) + stack.count;
        }

        data.items.Clear();
        foreach (string itemID in listOrder)
        {
            data.items.Add(new ItemStack(itemID, dicCount[itemID]));
        }

        HashSet<string> setFlag = new();
        List<string> listFlag = new();
        foreach (string flag in data.flags)
        {
            string renamed = GetRenamedKey(flag);
            if (setFlag.Add(renamed))
                listFlag.Add(renamed);
        }

        data.flags = listFlag;
    }

    private static string GetRenamedKey(string key)
    {
        if (key == null)
            return null;

        if (s_DicRenamedKey.TryGetValue(key, out string renamed))
            return renamed;

        // 버전 2에서 저장한 Region_Clear_Map_001_Field 같은 플래그
        if (key.StartsWith(OldClearPrefix, StringComparison.Ordinal))
            return "Clear_" + key.Substring(OldClearPrefix.Length);

        return key;
    }
}
