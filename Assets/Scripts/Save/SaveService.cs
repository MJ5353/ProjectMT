using System;
using System.IO;
using UnityEngine;

namespace MushTaker.Save
{
    /// <summary>
    /// JSON 파일 세이브. 임시 파일에 먼저 쓰고 바꿔 끼워서 저장 중 꺼져도 기존 파일이 깨지지 않게 한다.
    /// 읽기에 실패하면 깨진 파일을 .corrupt로 옮겨 두고 새 데이터로 시작한다.
    /// </summary>
    public class SaveService
    {
        readonly string _path;

        public SaveService(string fileName = "save.json")
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public string FilePath => _path;

        public SaveData Load()
        {
            if (!File.Exists(_path)) return new SaveData();
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path));
                return Migrate(data ?? new SaveData());
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 세이브를 읽지 못해 새로 시작합니다: {e.Message}");
                File.Copy(_path, _path + ".corrupt", true);
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            data.version = SaveData.CurrentVersion;
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(tmp, _path);
        }

        public void Delete()
        {
            if (File.Exists(_path)) File.Delete(_path);
        }

        static SaveData Migrate(SaveData data)
        {
            // 예: if (data.version < 2) { ...변환...; data.version = 2; }
            data.items ??= new();
            data.flags ??= new();
            data.version = SaveData.CurrentVersion;
            return data;
        }
    }
}
