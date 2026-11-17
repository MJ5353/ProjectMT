using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 모든 DataDef를 모아 ID로 찾는 데이터베이스. Assets/Resources/GameDatabase.asset 한 개만 둔다.
// !!) 목록은 에디터 메뉴(또는 플레이 시작 시 자동)로 채우므로 손으로 넣지 않는다
[CreateAssetMenu(menuName = "MushTaker/Data/Game Database", fileName = "GameDatabase")]
public class GameDatabase : ScriptableObject
{
    public const string ResourcePath = "GameDatabase";

    [SerializeField] private List<DataDef> m_ListDef = new();

    private Dictionary<string, DataDef> m_DicDefByID;

    public IReadOnlyList<DataDef> ListDef => m_ListDef;

    // ------------------------------------------------

    public void Init()
    {
        m_DicDefByID = new Dictionary<string, DataDef>();

        foreach (DataDef def in m_ListDef)
        {
            if (def == null)
                continue;

            if (m_DicDefByID.TryAdd(def.ID, def) == false)
                CLogger.Error("GameDatabase", $"ID 중복: {def.ID} ({def.name})", def);
        }
    }

    public T Get<T>(string id) where T : DataDef
    {
        if (TryGet(id, out T def))
            return def;

        CLogger.Warning("GameDatabase", $"{typeof(T).Name} '{id}'를 찾지 못했습니다.");
        return null;
    }

    public bool TryGet<T>(string id, out T result) where T : DataDef
    {
        if (m_DicDefByID == null)
            Init();

        result = null;
        if (string.IsNullOrEmpty(id))
            return false;

        if (m_DicDefByID.TryGetValue(id, out DataDef def) == false)
            return false;

        result = def as T;
        return result != null;
    }

    public IEnumerable<T> GetAll<T>() where T : DataDef => m_ListDef.OfType<T>();

#if UNITY_EDITOR
    public void EditorSetDefs(List<DataDef> listDef)
    {
        m_ListDef = listDef;
        m_DicDefByID = null;
    }
#endif
}
