using UnityEngine;
using TMPro;

// ==================================================

// 화면 위 보스 체력바. 보스가 대상을 잡으면(BossAggroed) 처음 잡은 보스 하나를 띄우고, 죽거나 새 판이 시작될(FlowBegan) 때까지 놓지 않는다.
// 체력은 UnitHpChanged, 분노 표시는 분노 포효(BossRoared.IsEnraged)로 받는다. 매 프레임 하는 일은 없다(이벤트만).
// 바 묶음(m_Panel)은 어그로 전엔 꺼 두므로, 이 컴포넌트는 늘 켜진 부모에 붙는다

public class ScreenBattle_BossBar : MonoBehaviour
{
    [SerializeField] private GameObject m_Panel = null;    // 이름·바·분노 표시 묶음. 어그로 전엔 꺼 둔다
    [SerializeField] private UIGauge m_Bar = null;
    [SerializeField] private TMP_Text m_BossName = null;
    [SerializeField] private GameObject m_EnrageIcon = null;

    private UnitController m_Boss;

    // ------------------------------------------------

    private void Awake()
    {
        m_Panel.E_SetActive(false);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Subscribe<UnitHpChanged>(OnUnitHpChanged);
        SEventManager.Subscribe<BossAggroed>(OnBossAggroed);
        SEventManager.Subscribe<BossRoared>(OnBossRoared);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Unsubscribe<UnitHpChanged>(OnUnitHpChanged);
        SEventManager.Unsubscribe<BossAggroed>(OnBossAggroed);
        SEventManager.Unsubscribe<BossRoared>(OnBossRoared);
    }

    // ---

    // 새 판(다시 하기 포함). 지난 판 보스는 이미 풀로 돌아갔다
    private void OnFlowBegan(FlowBegan evt)
    {
        if (evt.Flow is BattleFlow)
            Release();
    }

    // 어그로가 끌린 첫 보스를 잡는다
    private void OnBossAggroed(BossAggroed evt)
    {
        UnitController unit = evt.Unit;
        if (m_Boss != null || unit == null || unit.IsDead)
            return;

        m_Boss = unit;
        m_BossName.E_SetText(unit.Def.DisplayName);
        m_Bar.SetValue(unit.HealthF.Current, unit.HealthF.MaxHp, true);
        m_EnrageIcon.E_SetActive(unit.StateF.TryGetSub(out BossWatchSub watch) && watch.IsEnraged);
        m_Panel.E_SetActive(true);
    }

    // 잡은 보스만. 죽으면 내린다
    private void OnUnitHpChanged(UnitHpChanged evt)
    {
        if (m_Boss == null || evt.Unit != m_Boss)
            return;

        HealthFeature health = m_Boss.HealthF;
        if (health.IsDead)
        {
            Release();
            return;
        }

        m_Bar.SetValue(health.Current, health.MaxHp);
    }

    // 분노는 분노 포효 때 표시한다(분노하면 바로 포효를 예약한다, BossWatchSub)
    private void OnBossRoared(BossRoared evt)
    {
        if (m_Boss != null && evt.Unit == m_Boss && evt.IsEnraged)
            m_EnrageIcon.E_SetActive(true);
    }

    private void Release()
    {
        m_Boss = null;
        m_EnrageIcon.E_SetActive(false);
        m_Panel.E_SetActive(false);
    }
}
