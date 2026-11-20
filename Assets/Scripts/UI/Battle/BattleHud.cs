using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 HUD. 좌상단 밤 시계, 중앙 하단 체력, 공격·대시 아이콘(사용 중, 연계는 UISkillChain), 우상단 주운 버섯 목록(신선도별), 토벌 종료 버튼(현장 시장 팝업).
// 매 프레임 BattleService·플레이어를 읽어 그린다. 플레이어가 쓰러지면 부활/귀가 팝업을 띄운다

public class BattleHud : MonoBehaviour
{
    [SerializeField] private UINightClock m_Clock = null;
    [SerializeField] private UIGauge m_Hp = null;
    [SerializeField] private GameObject m_AttackActive = null; // 공격 중 테두리
    [SerializeField] private GameObject m_DashActive = null;   // 대시 중 테두리
    
    [SerializeField] private RectTransform m_LootList = null;   // 세로 목록(VerticalLayoutGroup)
    [SerializeField] private UILootRow m_LootRowTemplate = null; // 목록 밖에 꺼 둔 줄 원본
    [SerializeField] private Button m_ExitButton = null;
    [SerializeField] private DeathPopup m_DeathPopupPrefab = null;
    [SerializeField] private MarketPopup m_MarketPopupPrefab = null; // 토벌 종료 뒤 현장 시장. 없으면 시장 없이 끝낸다

    private int m_LastHp = -1;
    private int m_LastLoot = -1;
    private UIListPool<UILootRow> m_LootPool;
    private readonly List<string> m_ListLootID = new();               // 처음 주운 순서
    private readonly Dictionary<string, int> m_DicFresh = new();      // 버섯별 신선 개수
    private readonly Dictionary<string, int> m_DicStale = new();      // 버섯별 덜 신선 개수

    // ------------------------------------------------

    private void Awake()
    {
        m_LootPool = new UIListPool<UILootRow>(m_LootRowTemplate, m_LootList);
        m_ExitButton.onClick.AddListener(OnClickExit);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<PlayerDowned>(OnPlayerDowned);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<PlayerDowned>(OnPlayerDowned);
    }

    private void Update()
    {
        BattleService battleS = GameManager.Instance?.BattleS;
        if (battleS == null)
            return;

        m_Clock.SetTime(battleS.RemainTime, battleS.NightTime);
        m_ExitButton.interactable = battleS.State == EBattleState.Playing && battleS.IsPlayerDown == false;

        UpdatePlayer(battleS.Player);
        UpdateLoot(battleS);
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
        
        m_AttackActive.SetActive(isAttack);
        m_DashActive.SetActive(executor.Current == EActionType.Dash);
    }

    // 주운 개수가 바뀔 때만 목록을 다시 그린다
    private void UpdateLoot(BattleService battleS)
    {
        int total = 0;
        foreach (var pair in battleS.DicLoot)
        {
            total += pair.Value;
        }

        if (total == m_LastLoot)
            return;

        m_LastLoot = total;
        RefreshLootList(battleS.DicLoot);
    }

    // 주울 때만 다시 그린다. 버섯 하나에 한 줄("송이 신선 x2 / 덜 신선 x1"). 줄은 버리지 않고 다시 쓴다
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

        GameDatabase database = GameManager.Instance?.Database;
        int index = 0;
        
        foreach (string itemID in m_ListLootID)
        {
            MushroomDef def = null;
            database?.TryGet(itemID, out def);
            string displayName = def != null ? def.DisplayName : itemID;

            int fresh = m_DicFresh[itemID];
            int stale = m_DicStale[itemID];
            string text = fresh > 0 && stale > 0 ? $"{displayName} 신선 x{fresh} / 덜 신선 x{stale}" : 
                fresh > 0 ? $"{displayName} 신선 x{fresh}" : $"{displayName} 덜 신선 x{stale}";

            m_LootPool.Get(index).Set(def != null ? def.Icon : null, def != null ? def.Tint : UIStyle.PanelLight, text);
            
            index++;
        }

        m_LootPool.HideFrom(index);
    }

    // 시장을 열고 팝업을 띄운다
    private void OnClickExit()
    {
        GameManager gameM = GameManager.Instance;
        BattleService battleS = gameM?.BattleS;
        if (battleS == null)
            return;

        bool isMarket = m_MarketPopupPrefab != null;
        if (isMarket == false)
            CLogger.Warning("BattleHud", "시장 팝업 프리팹이 없어 시장 없이 끝냅니다. MushTaker/시장/시장 팝업 만들기를 실행하세요.");

        battleS.ExitHunt(isMarket);
        if (battleS.State == EBattleState.Market)
            gameM.PopupS?.Open(m_MarketPopupPrefab);
    }

    private void OnPlayerDowned(PlayerDowned evt)
    {
        GameManager.Instance?.PopupS?.Open(m_DeathPopupPrefab);
    }
}
