using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 화면(HUD). 좌상단 밤 시계, 중앙 하단 체력, 공격·대시 아이콘(사용 중, 연계는 ScreenBattle_SkillChain), 우상단 주운 버섯 목록(신선도별), 토벌 종료 버튼(현장 시장 팝업).
// 매 프레임 BattleService·플레이어를 읽어 그린다. 사망·시장·결과 팝업은 BattleFlow 상태가 띄우고, 여기서는 토벌 종료 요청만 보낸다(확인 팝업도 BattleFlow가 띄운다)

public class ScreenBattle : ScreenBase
{
    [SerializeField] private UINightClock m_Clock = null;
    [SerializeField] private UIGauge m_Hp = null;
    [SerializeField] private GameObject m_AttackActive = null; // 공격 중 테두리
    [SerializeField] private GameObject m_DashActive = null;   // 대시 중 테두리
    [SerializeField] private Button m_ExitButton = null;
    [SerializeField] private UIListView<ScreenBattle_LootRow> m_LootPool = new(); // 세로 목록

    private readonly List<string> m_ListLootID = new();               // 처음 주운 순서. m_LootPool 순서
    private readonly Dictionary<string, int> m_DicFresh = new();      // 버섯별 신선 개수
    private readonly Dictionary<string, int> m_DicStale = new();      // 버섯별 덜 신선 개수
    
    private int m_LastHp = -1;

    // ------------------------------------------------

    private void Awake()
    {
        m_LootPool.Init(EUIComponentType.ScreenBattle_LootRow);
        m_ExitButton.E_SetOnClick(OnClickExit);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Subscribe<BattleStateChanged>(OnBattleStateChanged);
        SEventManager.Subscribe<LootChanged>(OnLootChanged);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Unsubscribe<BattleStateChanged>(OnBattleStateChanged);
        SEventManager.Unsubscribe<LootChanged>(OnLootChanged);
    }

    private void Update()
    {
        BattleFlow battle = FlowManager.Instance.Battle;
        if (battle == null)
            return;

        BattleService battleS = battle.BattleS;
        m_Clock.SetTime(battleS.RemainTime, battleS.NightTime);

        UpdatePlayer(battleS.Player);
    }

    // ---

    private void UpdatePlayer(UnitController player)
    {
        if (player == null)
            return;

        HealthFeature health = player.HealthF;
        if (health.Current != m_LastHp)
        {
            m_Hp.SetValue(health.Current, health.MaxHp, m_LastHp < 0);
            m_LastHp = health.Current;
        }

        UnitExecutor executor = player.UnitExecutor;
        bool isAttack = executor.Current == EActionType.Attack;
        
        m_AttackActive.E_SetActive(isAttack);
        m_DashActive.E_SetActive(executor.Current == EActionType.Dash);
    }

    // 주울 때·떨굴 때만(LootChanged) 다시 그린다. 버섯 하나에 한 줄("송이 신선 x2 / 덜 신선 x1"). 줄은 버리지 않고 다시 쓴다
    private void RefreshLootList(IReadOnlyDictionary<LootKey, int> dicLoot)
    {
        m_ListLootID.Clear();
        m_DicFresh.Clear();
        m_DicStale.Clear();
        
        foreach (var pair in dicLoot)
        {
            string itemID = pair.Key.ItemID;
            if (m_DicFresh.ContainsKey(itemID) == false)
            {
                m_ListLootID.Add(itemID);
                m_DicFresh[itemID] = 0;
                m_DicStale[itemID] = 0;
            }

            if (pair.Key.Freshness == EFreshness.High)
                m_DicFresh[itemID] += pair.Value;
            else
                m_DicStale[itemID] += pair.Value;
        }

        m_LootPool.Refresh(m_ListLootID, SetLootRow);
    }

    private void SetLootRow(ScreenBattle_LootRow row, string itemID)
    {
        string displayName = itemID;
        Sprite icon = null;
        Color tint = UIStyle.PanelLight;

        if (GameDatabase.S_TryGet(itemID, out MushroomDef def))
        {
            displayName = def.DisplayName;
            icon = def.Icon;
            tint = def.Tint;
        }

        int fresh = m_DicFresh[itemID];
        int stale = m_DicStale[itemID];
        
        string text = fresh > 0 && stale > 0 ?
            StringDef.S_Format(EStringKey.Battle_LootBothFormat, displayName, fresh, stale) :
            fresh > 0 ?
                StringDef.S_Format(EStringKey.Battle_LootFreshFormat, displayName, fresh) :
                StringDef.S_Format(EStringKey.Battle_LootStaleFormat, displayName, stale);
        
        row.Set(icon, tint, text);
    }

    // on action ---
    
    // 확인 팝업·일시정지·시장 전환은 BattleFlow가 맡는다
    private void OnClickExit() => FlowManager.Instance.Battle?.ExitHunt();

    // 새 판 시작(씬 진입·다시 하기). 체력바는 처음부터 다시 채우고, 주운 목록은 비운다
    private void OnFlowBegan(FlowBegan evt)
    {
        if (evt.Flow is not BattleFlow battle)
            return;

        m_LastHp = -1;
        RefreshLootList(battle.BattleS.DicLoot);
    }

    // 토벌 종료는 Play에서만 누를 수 있다
    private void OnBattleStateChanged(BattleStateChanged evt)
    {
        m_ExitButton.E_SetInteractable(evt.State == EBattleFlowState.Play);
    }

    private void OnLootChanged(LootChanged evt)
    {
        RefreshLootList(FlowManager.Instance.Battle.BattleS.DicLoot);
    }
}
