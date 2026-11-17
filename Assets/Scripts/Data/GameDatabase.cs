using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MushTaker.Data
{
    /// <summary>
    /// 모든 DataDef를 모아 id로 찾는 데이터베이스. Assets/Resources/GameDatabase.asset 한 개만 둔다.
    /// 목록은 에디터 메뉴(또는 플레이 시작 시 자동)로 Assets 안의 DataDef를 모아 채우므로 손으로 넣지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MushTaker/Data/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "GameDatabase";

        [SerializeField] List<DataDef> defs = new();

        Dictionary<string, DataDef> _byId;

        public IReadOnlyList<DataDef> All => defs;

        public void Initialize()
        {
            _byId = new Dictionary<string, DataDef>();
            foreach (var def in defs)
            {
                if (def == null) continue;
                if (!_byId.TryAdd(def.Id, def))
                    Debug.LogError($"[GameDatabase] id 중복: {def.Id} ({def.name})", def);
            }
        }

        public T Get<T>(string id) where T : DataDef
        {
            if (_byId == null) Initialize();
            if (_byId.TryGetValue(id, out var def) && def is T typed) return typed;
            Debug.LogWarning($"[GameDatabase] {typeof(T).Name} '{id}'를 찾지 못했습니다.");
            return null;
        }

        public bool TryGet<T>(string id, out T result) where T : DataDef
        {
            if (_byId == null) Initialize();
            result = _byId.TryGetValue(id, out var def) ? def as T : null;
            return result != null;
        }

        public IEnumerable<T> GetAll<T>() where T : DataDef => defs.OfType<T>();

#if UNITY_EDITOR
        public void EditorSetDefs(List<DataDef> list)
        {
            defs = list;
            _byId = null;
        }
#endif
    }
}
