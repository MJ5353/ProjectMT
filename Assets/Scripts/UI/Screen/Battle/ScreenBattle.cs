using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 화면(HUD). 좌상단 밤 시계, 중앙 하단 체력, 공격·대시 아이콘(사용 중, 연계는 ScreenBattle_SkillChain), 우상단 주운 버섯 목록(신선도별), 토벌 종료 버튼(현장 시장 팝업).
// 매 프레임 BattleService·플레이어를 읽어 그린다. 플레이어가 쓰러지면 부활/귀가 팝업을 띄운다

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
        SEventManager.Subscribe<PlayerDowned>(OnPlayerDowned);
        SEventManager.Subscribe<BattleEnded>(OnBattleEnded);
        SEventManager.Subscribe<LootChanged>(OnLootChanged);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<PlayerDowned>(OnPlayerDowned);
        SEventManager.Unsubscribe<BattleEnded>(OnBattleEnded);
        SEventManager.Unsubscribe<LootChanged>(OnLootChanged);
    }

    private void Update()
    {
        BattleService battleS = GameManager.Instance.BattleS;
        if (battleS == null)
            return;

        m_Clock.SetTime(battleS.RemainTime, battleS.NightTime);
        m_ExitButton.E_SetInteractable(battleS.State == EBattleState.Playing && battleS.IsPlayerDown == false);

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
    
    // 바로 나가지 않고 "토벌을 종료할까요?"를 묻는다. 묻는 동안 게임을 멈춘다. 확인 팝업 프리팹이 없으면 묻지 않고 나간다
    private void OnClickExit()
    {
        PopupConfirm confirm = GameManager.Instance.PopupS?.Open<PopupConfirm>(EPopupType.PopupConfirm);
        if (confirm == null)
        {
            ExitHunt();
            return;
        }

        TimeManager.Instance.SetPause(true);
        confirm.Set(EStringKey.Battle_ExitConfirm, OnConfirmExit, OnCancelExit);
    }

    // 멈춤을 먼저 풀고 나간다(시장이 열리면 BattleService가 다시 멈춘다)
    private void OnConfirmExit()
    {
        TimeManager.Instance.SetPause(false);
        ExitHunt();
    }

    private void OnCancelExit()
    {
        TimeManager.Instance.SetPause(false);
    }

    // 시장을 열고 팝업(PopupMarket)을 띄운다. 시장 프리팹이 없으면 시장 없이 끝낸다
    private void ExitHunt()
    {
        GameManager gameM = GameManager.Instance;
        BattleService battleS = gameM.BattleS;
        
        if (battleS == null)
            return;

        bool isMarket = gameM.PopupS != null && gameM.PopupS.HasPrefab(EPopupType.PopupMarket);
        battleS.ExitHunt(isMarket);
        
        if (battleS.State == EBattleState.Market)
            gameM.PopupS?.Open<PopupMarket>(EPopupType.PopupMarket);
    }

    private void OnLootChanged(LootChanged evt)
    {
        RefreshLootList(GameManager.Instance.BattleS.DicLoot);
    }

    private void OnPlayerDowned(PlayerDowned evt)
    {
        GameManager.Instance.PopupS?.Open<PopupDeath>(EPopupType.PopupDeath);
    }

    // 시장 팝업은 MarketClosed로, 사망 팝업은 귀가(GiveUp) 전에 스스로 닫힌다
    private void OnBattleEnded(BattleEnded evt)
    {
        GameManager.Instance.PopupS?.Open<PopupResult>(EPopupType.PopupResult);
    }
}
