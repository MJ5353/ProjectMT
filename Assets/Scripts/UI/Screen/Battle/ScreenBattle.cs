using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 화면(HUD). 좌상단 밤 시계, 중앙 하단 체력, 공격·대시 아이콘(사용 중, 연계는 ScreenBattle_SkillChain), 우상단 주운 버섯 목록(신선도별), 토벌 종료 버튼(현장 시장 팝업), 보스 포효 연출.
// 시계만 매 프레임 그리고, 체력(UnitHpChanged)·아이콘(StateFeature.Started)·포효(BossRoared)는 이벤트로 그린다.
// 사망·시장·결과 팝업은 BattleFlow 상태가 띄우고, 여기서는 토벌 종료 요청만 보낸다(확인 팝업도 BattleFlow가 띄운다)

public class ScreenBattle : ScreenBase
{
    [SerializeField] private UINightClock m_Clock = null;
    [SerializeField] private UIGauge m_Hp = null;
    [SerializeField] private GameObject m_AttackActive = null; // 공격 중 테두리
    [SerializeField] private GameObject m_DashActive = null;   // 대시 중 테두리
    [SerializeField] private Button m_ExitButton = null;
    [SerializeField] private UIListView<ScreenBattle_LootRow> m_LootPool = new(); // 세로 목록
    [SerializeField] private CanvasGroup m_RoarVignette = null; // 포효: 붉은 가장자리(꽉 찬 이미지)
    [SerializeField] private CanvasGroup m_RoarBanner = null;   // 포효: 이름 띠
    [SerializeField] private TMP_Text m_RoarText = null;

    private const float RoarFadeRatio = 0.2f; // 포효 전체 시간 중 나타나고 사라지는 구간 비율(앞뒤 각각)

    private readonly List<string> m_ListLootID = new();               // 처음 주운 순서. m_LootPool 순서
    private readonly Dictionary<string, int> m_DicFresh = new();      // 버섯별 신선 개수
    private readonly Dictionary<string, int> m_DicStale = new();      // 버섯별 덜 신선 개수
    private readonly UISequence m_RoarSequence = new();

    private UnitController m_Player;   // 이번 판 플레이어. StateF 구독은 풀 재사용으로 남으니 다음 판·OnDisable에서 푼다

    // ------------------------------------------------

    private void Awake()
    {
        m_LootPool.Init(EUIComponentType.ScreenBattle_LootRow);
        m_ExitButton.E_SetOnClick(OnClickExit);
        m_RoarVignette.E_SetVisible(false);
        m_RoarBanner.E_SetVisible(false);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Subscribe<BattleStateChanged>(OnBattleStateChanged);
        SEventManager.Subscribe<LootChanged>(OnLootChanged);
        SEventManager.Subscribe<UnitHpChanged>(OnUnitHpChanged);
        SEventManager.Subscribe<BossRoared>(OnBossRoared);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Unsubscribe<BattleStateChanged>(OnBattleStateChanged);
        SEventManager.Unsubscribe<LootChanged>(OnLootChanged);
        SEventManager.Unsubscribe<UnitHpChanged>(OnUnitHpChanged);
        SEventManager.Unsubscribe<BossRoared>(OnBossRoared);

        BindPlayer(null);
        m_RoarSequence.Stop();
        m_RoarVignette.alpha = 0f; // 종료 중엔 GameManager가 먼저 사라질 수 있어 Define을 읽지 않는다
        m_RoarBanner.alpha = 0f;
    }

    private void Update()
    {
        BattleFlow battle = FlowManager.Instance.Battle;
        if (battle == null)
            return;

        BattleService battleS = battle.BattleS;
        m_Clock.SetTime(battleS.RemainTime, battleS.NightTime);
    }

    // ---

    // 플레이어를 바꿔 잡는다. 지난 플레이어 StateF 구독을 풀고, 새 플레이어의 체력·행동을 바로 그린다
    private void BindPlayer(UnitController player)
    {
        if (m_Player != null)
            m_Player.StateF.Started -= OnPlayerActionStarted;

        m_Player = player;
        if (player == null)
            return;

        player.StateF.Started += OnPlayerActionStarted;
        m_Hp.SetValue(player.HealthF.Current, player.HealthF.MaxHp, true);
        OnPlayerActionStarted(player.StateF.Current);
    }

    private void SetRoarAlpha(float alpha)
    {
        m_RoarVignette.alpha = alpha * GameManager.Instance.Define.Hud.RoarVignetteAlpha;
        m_RoarBanner.alpha = alpha;
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

    // 새 판 시작(씬 진입·다시 하기). 플레이어를 다시 잡아 체력바를 바로 채우고, 주운 목록은 비운다
    private void OnFlowBegan(FlowBegan evt)
    {
        if (evt.Flow is not BattleFlow battle)
            return;

        BindPlayer(battle.BattleS.Player);
        RefreshLootList(battle.BattleS.DicLoot);
    }

    private void OnUnitHpChanged(UnitHpChanged evt)
    {
        if (m_Player == null || evt.Unit != m_Player)
            return;

        m_Hp.SetValue(m_Player.HealthF.Current, m_Player.HealthF.MaxHp);
    }

    private void OnPlayerActionStarted(ECoreState action)
    {
        m_AttackActive.E_SetActive(action == ECoreState.Attack);
        m_DashActive.E_SetActive(action == ECoreState.Dash);
    }

    // 붉은 가장자리·이름 띠가 앞 20% 동안 나타나고 끝 20% 동안 사라진다. 히트스톱·일시정지와 상관없이 실제 시간(UISequence)
    private void OnBossRoared(BossRoared evt)
    {
        if (evt.Unit == null)
            return;

        string strName = evt.Unit.Def.DisplayName;
        m_RoarText.E_SetText(evt.IsEnraged ? StringDef.S_Format(EStringKey.Battle_RoarEnragedFormat, strName) : strName);

        float total = GameManager.Instance.Define.Hud.RoarEffectTime;
        float fade = total * RoarFadeRatio;

        m_RoarSequence.Clear()
            .Tween(fade, t => SetRoarAlpha(t))
            .Wait(total - fade * 2f)
            .Tween(fade, t => SetRoarAlpha(1f - t))
            .Play(this);
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
