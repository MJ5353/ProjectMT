using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 머리 위 몬스터 체력바. 잡몹·엘리트가 맞으면(UnitHpChanged) 머리 위에 작은 바가 잠깐(Define.Hud.MonsterHpBarShowTime) 뜬다.
// 매 프레임은 떠 있는 바의 위치·시간만 본다(없으면 쉰다). 보스는 화면 위 큰 바(ScreenBattle_BossBar)만 쓴다.
// 바 묶음(Bar)은 버리지 않고 m_StackFreeBar에 돌려 다시 쓴다
// 머리 위 바는 UIReuseService로 꺼내고, 유닛이 지워지면(UnitService.UnitRemoved) 돌려준다. 새 판이 시작되면(FlowBegan, 다시 하기 포함) 남은 바를 모두 돌려주고 새 UnitService에 다시 붙는다.

public class ScreenBattle_UnitHpBars : MonoBehaviour
{
    [SerializeField] private RectTransform m_Root = null;      // 머리 위 바를 놓을 캔버스 영역(꽉 찬 RectTransform)

    private readonly List<Bar> m_ListBar = new(); // 떠 있는 머리 위 바
    private readonly Stack<Bar> m_StackFreeBar = new(); // 돌려받은 바 묶음(다시 쓴다)

    private class Bar
    {
        public UnitController Unit;
        public int UnitID;
        public UIGauge Gauge;
        public float Remain;
    }

    // ------------------------------------------------

    private void OnEnable()
    {
        SEventManager.Subscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Subscribe<UnitHpChanged>(OnUnitHpChanged);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<FlowBegan>(OnFlowBegan);
        SEventManager.Unsubscribe<UnitHpChanged>(OnUnitHpChanged);
    }

    private void LateUpdate()
    {
        if (m_ListBar.Count == 0 || FlowManager.Instance.Battle == null)
            return;

        Camera cam = CameraManager.Instance.Main;
        if (cam == null)
            return;

        UpdateBars(cam, GameManager.Instance.Define, Time.unscaledDeltaTime);
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
                RemoveBar(i);
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

        for (int i = m_ListBar.Count - 1; i >= 0; --i)
        {
            RemoveBar(i);
        }

        battle.UnitS.UnitRemoved += OnUnitRemoved;
    }

    // 체력이 줄면 머리 위 바를 띄우거나(줄기 전 값에서 시작해야 잔상이 보인다) 시간을 다시 채운다. 보스는 큰 바만(ScreenBattle_BossBar)
    private void OnUnitHpChanged(UnitHpChanged evt)
    {
        UnitController unit = evt.Unit;
        if (unit == null)
            return;

        HealthFeature health = unit.HealthF;
        if (health.Current >= evt.Previous || unit.Def is not MonsterDef monster || monster.IsPet || IsBoss(unit))
            return;

        int index = FindBarIndex(unit.UnitID);
        Bar bar = index >= 0 ? m_ListBar[index] : null;
        if (bar == null)
        {
            UIGauge gauge = FlowManager.Instance.UIReuseS?.Spawn<UIGauge>(EUIComponentType.ScreenBattle_UnitHpBar, m_Root);
            if (gauge == null)
                return;

            gauge.SetValue(evt.Previous, health.MaxHp, true);
            bar = m_StackFreeBar.Count > 0 ? m_StackFreeBar.Pop() : new Bar();
            bar.Unit = unit;
            bar.UnitID = unit.UnitID;
            bar.Gauge = gauge;
            m_ListBar.Add(bar);
        }

        bar.Remain = GameManager.Instance.Define.Hud.MonsterHpBarShowTime;
        bar.Gauge.SetValue(health.Current, health.MaxHp);
    }

    // 지워진 유닛의 바를 돌려놓는다
    private void OnUnitRemoved(int unitID)
    {
        int index = FindBarIndex(unitID);
        if (index >= 0)
            RemoveBar(index);
    }

    // 람다 없이 찾는다(Find·FindIndex는 부를 때마다 캡처 할당)
    private int FindBarIndex(int unitID)
    {
        for (int i = 0; i < m_ListBar.Count; ++i)
        {
            if (m_ListBar[i].UnitID == unitID)
                return i;
        }

        return -1;
    }

    // 게이지를 돌려주고 바 묶음은 비워서 다시 쓴다
    private void RemoveBar(int index)
    {
        Bar bar = m_ListBar[index];
        m_ListBar.RemoveAt(index);

        if (bar.Gauge != null)
            FlowManager.Instance.UIReuseS?.Despawn(bar.Gauge);

        bar.Unit = null;
        bar.UnitID = 0;
        bar.Gauge = null;
        bar.Remain = 0f;
        m_StackFreeBar.Push(bar);
    }

    // 보스 판정은 보스 감시 서브 유무(AIDef). 보스는 화면 위 큰 바가 맡는다
    private static bool IsBoss(UnitController unit)
    {
        return unit.StateF.TryGetSub(out BossWatchSub _);
    }
}
