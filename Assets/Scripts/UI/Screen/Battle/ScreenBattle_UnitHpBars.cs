using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ==================================================

// 몬스터 체력바. 잡몹·엘리트는 맞으면 머리 위에 작은 바가 잠깐(Define.Hud.MonsterHpBarShowTime) 뜨고,
// 보스는 어그로가 끌리면(BossAggroed) 화면 위에 큰 바가 뜬다(분노하면 분노 표시). 체력은 UnitHpChanged로 받고, 매 프레임은 떠 있는 바 위치·시간과 보스만 본다.
// 머리 위 바는 UIReuseService로 꺼내고, 유닛이 지워지면(UnitService.UnitRemoved) 돌려준다. 새 판이 시작되면(FlowBegan, 다시 하기 포함) 남은 바를 모두 돌려주고 새 UnitService에 다시 붙는다.

public class ScreenBattle_UnitHpBars : MonoBehaviour
{
    [SerializeField] private RectTransform m_Root = null;      // 머리 위 바를 놓을 캔버스 영역(꽉 찬 RectTransform)
    [SerializeField] private GameObject m_BossRoot = null;     // 보스 바 묶음. 어그로 전엔 꺼 둔다
    [SerializeField] private UIGauge m_BossBar = null;
    [SerializeField] private TMP_Text m_BossName = null;
    [SerializeField] private GameObject m_EnrageIcon = null;

    private readonly List<Bar> m_ListBar = new(); // 떠 있는 머리 위 바

    private UnitController m_Boss;

    private class Bar
    {
        public UnitController Unit;
        public int UnitID;
        public UIGauge Gauge;
        public float Remain;
    }

    // ------------------------------------------------

    private void Awake()
    {
        m_BossRoot.E_SetActive(false);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Subscribe<UnitHpChanged>(OnUnitHpChanged);
        SEventManager.Subscribe<BossAggroed>(OnBossAggroed);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Unsubscribe<UnitHpChanged>(OnUnitHpChanged);
        SEventManager.Unsubscribe<BossAggroed>(OnBossAggroed);
    }

    private void LateUpdate()
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        Camera cam = CameraManager.Instance.Main;
        
        if (units == null || cam == null)
            return;

        UpdateBars(cam, GameManager.Instance.Define, Time.unscaledDeltaTime);
        UpdateBoss();
    }

    // ---

    // 떠 있는 바만 머리 위를 따라다니고, 시간이 다 되거나 죽거나 카메라 뒤면 돌려준다
    private void UpdateBars(Camera cam, DefineDef define, float deltaTime)
    {
        for (int i = m_ListBar.Count - 1; i >= 0; i--)
        {
            Bar bar = m_ListBar[i];
            bar.Remain -= deltaTime;

            if (bar.Remain <= 0f || bar.Unit == null || bar.Unit.IsDead || TryPlace(bar.Gauge, bar.Unit, cam, define) == false)
            {
                Release(bar);
                m_ListBar.RemoveAt(i);
            }
        }
    }

    // 몸(캐릭터 컨트롤러) 맨 위보다 조금 위를 화면 좌표로 옮긴다. 카메라 뒤면 false
    private bool TryPlace(UIGauge gauge, UnitController unit, Camera cam, DefineDef define)
    {
        CharacterController body = unit.HurtBody;
        float top = 2f;
        if (body != null)
            top = (body.center.y + body.height * 0.5f) * unit.transform.lossyScale.y;

        Vector3 screen = cam.WorldToScreenPoint(unit.transform.position + Vector3.up * (top + define.Hud.MonsterHpBarOffset));
        if (screen.z <= 0f)
            return false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Root, screen, null, out Vector2 local);
        gauge.RectTransform.anchoredPosition = local;
        return true;
    }

    // 새 판의 UnitService에 붙는다. 유닛 번호는 UnitService가 판을 넘어 이어 세지만, 지난 판 유닛은 이미 풀로 돌아갔으니 바는 모두 돌려준다.
    // UnitRemoved 해제는 따로 하지 않는다(판이 끝날 때 UnitService.Clear가 구독을 비운다)
    private void OnFlowBegan(FlowBegan evt)
    {
        if (evt.Flow is not BattleFlow battle)
            return;

        foreach (Bar bar in m_ListBar)
        {
            Release(bar);
        }

        m_ListBar.Clear();
        ReleaseBoss();

        battle.UnitS.UnitRemoved += OnUnitRemoved;
    }

    // 체력이 줄면 머리 위 바를 띄우거나(줄기 전 값에서 시작해야 잔상이 보인다) 시간을 다시 채운다. 보스는 큰 바만
    private void OnUnitHpChanged(UnitHpChanged evt)
    {
        UnitController unit = evt.Unit;
        if (unit == null)
            return;

        HealthFeature health = unit.HealthF;
        if (unit == m_Boss)
        {
            m_BossBar.SetValue(health.Current, health.MaxHp);
            return;
        }

        if (health.Current >= evt.Previous || unit.Def is not MonsterDef monster || monster.IsPet || IsBoss(unit))
            return;

        Bar bar = m_ListBar.Find(shown => shown.Unit == unit);
        if (bar == null)
        {
            UIGauge gauge = FlowManager.Instance.UIReuseS?.Spawn<UIGauge>(EUIComponentType.ScreenBattle_UnitHpBar, m_Root);
            if (gauge == null)
                return;

            gauge.SetValue(evt.Previous, health.MaxHp, true);
            bar = new Bar { Unit = unit, UnitID = unit.UnitID, Gauge = gauge };
            m_ListBar.Add(bar);
        }

        bar.Remain = GameManager.Instance.Define.Hud.MonsterHpBarShowTime;
        bar.Gauge.SetValue(health.Current, health.MaxHp);
    }

    // 지워진 유닛의 바를 돌려놓고, 잡고 있던 보스면 놓는다
    private void OnUnitRemoved(int unitID)
    {
        int index = m_ListBar.FindIndex(bar => bar.UnitID == unitID);
        if (index >= 0)
        {
            Release(m_ListBar[index]);
            m_ListBar.RemoveAt(index);
        }

        if (m_Boss.E_Alive()?.UnitID == unitID)
            ReleaseBoss();
    }

    private static void Release(Bar bar)
    {
        if (bar.Gauge == null)
            return;

        FlowManager.Instance.UIReuseS?.Despawn(bar.Gauge);
        bar.Gauge = null;
    }

    // boss ---

    private static bool IsBoss(UnitController unit)
    {
        return unit.StateF.TryGetSub(out BossWatchSub _);
    }

    // 어그로가 끌린 첫 보스를 잡고, 죽거나 사라질 때까지 놓지 않는다
    private void OnBossAggroed(BossAggroed evt)
    {
        UnitController unit = evt.Unit;
        if (m_Boss != null || unit == null || unit.IsDead)
            return;

        m_Boss = unit;
        m_BossName.E_SetText(unit.Def.DisplayName);
        m_BossBar.SetValue(unit.HealthF.Current, unit.HealthF.MaxHp, true);
        m_BossRoot.E_SetActive(true);
    }

    private void UpdateBoss()
    {
        if (m_Boss == null)
            return;

        if (m_Boss.IsDead)
        {
            ReleaseBoss();
            return;
        }

        m_EnrageIcon.E_SetActive(m_Boss.StateF.TryGetSub(out BossWatchSub watch) && watch.IsEnraged);
    }

    private void ReleaseBoss()
    {
        m_Boss = null;
        m_BossRoot.E_SetActive(false);
    }
}
