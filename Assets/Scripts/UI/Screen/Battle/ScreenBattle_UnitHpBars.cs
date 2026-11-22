using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ==================================================

// 몬스터 체력바. 잡몹·엘리트는 맞으면 머리 위에 작은 바가 잠깐(Define.MonsterHpBarShowTime) 뜨고,
// 보스는 어그로가 끌리면 화면 위에 큰 바가 뜬다(분노하면 분노 표시). 매 프레임 UnitService의 유닛을 읽어 그린다.
// 머리 위 바는 UIReuseService로 꺼내고, 유닛이 지워지면(UnitService.UnitRemoved) 돌려준다(씬이 바뀌면 UIReuseService가 남은 바를 회수).

public class ScreenBattle_UnitHpBars : MonoBehaviour
{
    [SerializeField] private RectTransform m_Root = null;      // 머리 위 바를 놓을 캔버스 영역(꽉 찬 RectTransform)
    [SerializeField] private GameObject m_BossRoot = null;     // 보스 바 묶음. 어그로 전엔 꺼 둔다
    [SerializeField] private UIGauge m_BossBar = null;
    [SerializeField] private TMP_Text m_BossName = null;
    [SerializeField] private GameObject m_EnrageIcon = null;

    private readonly Dictionary<int, Bar> m_DicBar = new();

    private UnitController m_Boss;
    private int m_BossLastHp = -1;

    private class Bar
    {
        public UIGauge Gauge;
        public int LastHp;
        public float Remain;
    }

    // ------------------------------------------------

    private void Awake()
    {
        m_BossRoot.E_SetActive(false);
    }

    // Flow는 씬 컨트롤러 Start(-1100)에서 시작되므로 Start에서 구독한다. 해제는 따로 하지 않는다(씬이 닫힐 때 UnitService.Clear가 구독을 비운다)
    private void Start()
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units != null)
            units.UnitRemoved += OnUnitRemoved;
    }

    private void LateUpdate()
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        Camera cam = CameraManager.Instance.Main;
        
        if (units == null || cam == null)
            return;

        DefineDef define = GameManager.Instance.Define;
        float deltaTime = Time.unscaledDeltaTime;

        foreach (UnitController unit in units.ListUnit)
        {
            if (unit == null || unit.Def is not MonsterDef monster || monster.IsPet)
                continue;

            if (IsBoss(unit))
            {
                UpdateBossTarget(unit);
                continue;
            }

            UpdateBar(unit, cam, define, deltaTime);
        }

        UpdateBoss();
    }

    // ---

    // 체력이 줄면 보이기 시작해 정해진 시간 동안 머리 위를 따라다닌다
    private void UpdateBar(UnitController unit, Camera cam, DefineDef define, float deltaTime)
    {
        int hp = unit.HealthF.Current;
        if (m_DicBar.TryGetValue(unit.UnitID, out Bar bar) == false)
        {
            bar = new Bar { LastHp = hp };
            m_DicBar.Add(unit.UnitID, bar);
        }

        if (hp < bar.LastHp)
        {
            bar.Remain = define.MonsterHpBarShowTime;
            if (bar.Gauge == null)
            {
                bar.Gauge = FlowManager.Instance.UIReuseS?.Spawn<UIGauge>(EUIComponentType.ScreenBattle_UnitHpBar, m_Root);
                bar.Gauge.E_Alive()?.SetValue(bar.LastHp, unit.HealthF.MaxHp, true); // 줄기 전 값에서 시작해야 잔상이 보인다
            }

            bar.Gauge.E_Alive()?.SetValue(hp, unit.HealthF.MaxHp);
        }

        bar.LastHp = hp;
        if (bar.Gauge == null)
            return;

        bar.Remain -= deltaTime;
        if (bar.Remain <= 0f || unit.IsDead || TryPlace(bar.Gauge, unit, cam, define) == false)
        {
            Release(bar);
            return;
        }
    }

    // 몸(캐릭터 컨트롤러) 맨 위보다 조금 위를 화면 좌표로 옮긴다. 카메라 뒤면 false
    private bool TryPlace(UIGauge gauge, UnitController unit, Camera cam, DefineDef define)
    {
        CharacterController body = unit.HurtBody;
        float top = 2f;
        if (body != null)
            top = (body.center.y + body.height * 0.5f) * unit.transform.lossyScale.y;

        Vector3 screen = cam.WorldToScreenPoint(unit.transform.position + Vector3.up * (top + define.MonsterHpBarOffset));
        if (screen.z <= 0f)
            return false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Root, screen, null, out Vector2 local);
        gauge.RectTransform.anchoredPosition = local;
        return true;
    }

    // 지워진 유닛의 바를 돌려놓고, 잡고 있던 보스면 놓는다
    private void OnUnitRemoved(int unitID)
    {
        if (m_DicBar.Remove(unitID, out Bar bar))
            Release(bar);

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
        return unit.BrainBase is AIBrain ai && ai.Def != null && ai.Def.Type == EAIType.Boss;
    }

    // 어그로가 끌린 첫 보스를 잡고, 죽거나 사라질 때까지 놓지 않는다
    private void UpdateBossTarget(UnitController unit)
    {
        if (m_Boss != null || unit.IsDead || unit.BrainBase is not AIBrain ai || ai.TargetSelector.HasTarget == false)
            return;

        m_Boss = unit;
        m_BossLastHp = -1;
        m_BossName.E_SetText(unit.Def.DisplayName);
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

        int hp = m_Boss.HealthF.Current;
        if (hp != m_BossLastHp)
        {
            m_BossBar.SetValue(hp, m_Boss.HealthF.MaxHp, m_BossLastHp < 0);
            m_BossLastHp = hp;
        }

        m_EnrageIcon.E_SetActive(m_Boss.BrainBase is AIBrain ai && ai.IsEnraged);
    }

    private void ReleaseBoss()
    {
        m_Boss = null;
        m_BossRoot.E_SetActive(false);
    }
}
