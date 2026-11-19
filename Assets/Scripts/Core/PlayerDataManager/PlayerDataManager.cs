using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 플레이어 데이터 창구. 저장소(지금은 로컬 JSON, 나중에 서버)와 주고받는 값, 클라이언트에서 가공한 값, 지금 고른 지역 같은 현재값을 모아 둔다.
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneController가 Create)
public class PlayerDataManager : MonoSingleton<PlayerDataManager>
{
    // [mj] todo) server?
    private SaveService m_SaveService;

    private readonly List<PlayerDataBase> m_ListData = new();   // Init·Clear·Load·Save를 순회할 데이터. 등록 순서대로 실행

    public InventoryData Inventory { get; private set; }
    public FlagData Flag { get; private set; }
    public HuntData Hunt { get; private set; }

    // ==================================================

    public void RunSave()
    {
        m_SaveService.Save(ToSaveData());
    }

    public void ResetSave()
    {
        m_SaveService.Delete();
        ClearData();
    }

    // save ---

    private void Load(SaveData data)
    {
        ClearData();

        if (data == null)
            return;

        for (int i = 0; i < m_ListData.Count; ++i)
        {
            m_ListData[i].Load(data);
        }
    }

    private SaveData ToSaveData()
    {
        SaveData data = new SaveData();
        for (int i = 0; i < m_ListData.Count; ++i)
        {
            m_ListData[i].Save(data);
        }

        return data;
    }

    private void ClearData()
    {
        for (int i = 0; i < m_ListData.Count; ++i)
        {
            m_ListData[i].Clear();
        }
    }

    // 데이터를 순회 목록에 넣고 그대로 돌려준다
    private T AddData<T>(T data) where T : PlayerDataBase
    {
        m_ListData.Add(data);
        return data;
    }

    // virtual ---

    protected override void OnInit()
    {
        Inventory = AddData(new InventoryData());
        Flag = AddData(new FlagData());
        Hunt = AddData(new HuntData(Flag));

        for (int i = 0; i < m_ListData.Count; ++i)
        {
            m_ListData[i].Init();
        }

        m_SaveService = new SaveService();
        Load(m_SaveService.Load());
    }

    protected override void OnClear()
    {
        ClearData();
    }
}
