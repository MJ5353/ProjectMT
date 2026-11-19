
// ==================================================

// 보스 판단. MonsterBrain에 세 가지를 더한다. 공격 고르기(거리·쿨·가중치)는 그대로 데이터로 한다.
// 1. 쓰러짐(Down): 부위가 깨지면(PartF.Broken) 피격 경직이 끝난 뒤 PartBreak → Down, 그동안 공격하지 않는다
// 2. 포효(Roar): 처음 대상을 쫓기 시작할 때와 분노할 때 한 번
// 3. 분노: 체력이 Def.EnrageHpRate 이하가 되면 공용 쿨을 줄이고 분노 스킬을 후보에 더한다
// 맞아도 도망치지 않는다. 수치는 MonsterDef 보스 칸

public class BossBrain : MonsterBrain
{
    private bool m_IsRoarPending;   // 다음에 쫓을 때 포효
    private bool m_IsDownPending;   // 행동(피격 경직)이 끝나면 쓰러짐

    public bool IsEnraged { get; private set; }

    // ------------------------------------------------

    public BossBrain(MonsterController controller) : base(controller)
    {
        SetState(EAIState.Roar, new AIRoarState(this));
        SetState(EAIState.Down, new AIDownState(this));

        // 부위는 판단보다 먼저 붙는다(MonsterController.SetFeatures)
        if (controller.PartF != null)
            controller.PartF.Broken += OnPartBroken;
    }

    public override void Reset()
    {
        m_IsRoarPending = true;
        m_IsDownPending = false;
        IsEnraged = false;

        base.Reset();
    }

    // 쓰러짐 > 포효 순. 행동 중이면 끝날 때까지 미룬다
    protected override void Sense(float deltaTime)
    {
        base.Sense(deltaTime);
        if (Owner.IsDead)
            return;

        UpdateEnrage();
        if (IsActing())
            return;

        if (m_IsDownPending)
        {
            m_IsDownPending = false;
            Change(EAIState.Down);
            return;
        }

        if (m_IsRoarPending && State == EAIState.Chase)
        {
            m_IsRoarPending = false;
            Change(EAIState.Roar);
        }
    }

    // 쓰러져 있는 동안 맞아도 계속 누워 있는다(피격 경직이 끝나면 Down 애니로 돌아간다)
    protected override void OnActionEnded(EActionType action, bool isFinished)
    {
        if (action == EActionType.Hit && State == EAIState.Down)
            return;

        base.OnActionEnded(action, isFinished);
    }

    protected override EAIState GetStateAfterHit()
    {
        return EAIState.Chase;
    }

    // ==================================================

    private void UpdateEnrage()
    {
        if (IsEnraged || Def.EnrageHpRate <= 0f)
            return;

        HealthFeature health = Owner.HealthF;
        if (health.Current > health.MaxHp * Def.EnrageHpRate)
            return;

        IsEnraged = true;
        AttackChoice.Enrage(Def.ListEnrageSkill, Def.EnrageCooldownRate);
        m_IsRoarPending = true;
    }

    private void OnPartBroken(PartFeature.Part part)
    {
        if (Owner.IsDead == false)
            m_IsDownPending = true;
    }
}
