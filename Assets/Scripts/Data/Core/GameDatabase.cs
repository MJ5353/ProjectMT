using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ==================================================

// 모든 DataDef를 모아 ID로 찾는 데이터베이스. Assets/Resources/GameDatabase.asset 한 개만 둔다.
// !!) 목록은 에디터 메뉴(또는 플레이 시작 시 자동)로 채우므로 직접 수정하지 말 것

public class GameDatabase : ScriptableObject
{
    private readonly Dictionary<string, DataDef> m_DicDefByID = new();      // 런타임 조회용. Init에서 채운다
    
    public IReadOnlyList<DataDef> ListDef => m_ListDef;
    [SerializeField] private List<DataDef> m_ListDef = new();               // 에디터 메뉴가 채워 에셋에 저장한다(readonly면 저장되지 않음)

    // ------------------------------------------------

    public void Init()
    {
        m_DicDefByID.Clear();
        
        foreach (DataDef def in m_ListDef)
        {
            if (def == null)
                continue;

            if (m_DicDefByID.TryAdd(def.ID, def) == false)
                CLogger.Error("GameDatabase", $"ID 중복: {def.ID} ({def.name})", def);
        }
    }

    public T Get<T>(string dataID) where T : DataDef
    {
        if (TryGet(dataID, out T def))
            return def;

        CLogger.Warning("GameDatabase", $"{typeof(T).Name} '{dataID}'를 찾지 못했습니다.");
        return null;
    }

    public IEnumerable<T> GetAll<T>() where T : DataDef => m_ListDef.OfType<T>();

    public bool TryGet<T>(string dataID, out T result) where T : DataDef
    {
        if (m_DicDefByID.Count == 0)
            Init();

        result = null;

        if (dataID.E_IsNullOrEmpty() || m_DicDefByID.TryGetValue(dataID, out DataDef def) == false)
            return false;

        result = def as T;
        return result != null;
    }

    public static bool S_TryGet<T>(string dataID, out T result) where T : DataDef
    {
        result = null;
        
        var database = GameManager.Instance.Database;
        if (database.E_Alive() == false || 
            database.TryGet(dataID, out result) == false || result == null)
            return false;
        
        return true;
    }
    
#if UNITY_EDITOR
    // DataTools 메뉴 전용. 모은 목록으로 통째로 바꾼다
    public void EditorSetList(List<DataDef> listDef)
    {
        m_ListDef = listDef;
        m_DicDefByID.Clear();
    }
#endif
}
