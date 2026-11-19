using UnityEngine;

// ==================================================

// 판단 상태. 행동(공격·피격 등)은 행동 층(EActionType)이 맡고, 여기는 "무엇을 할지"만
public enum EAIState
{
    Idle,
    Wander,
    Chase,
    Flee,
    Dead,
}

// ==================================================

// 몬스터 판단 층. 노릴 적을 고르고(TargetFeature에 기억) AI 상태(대기·배회·추적·도망·사망 뒤)를 돌리며 행동 층(ActionLayer)에 요청만 한다.
// 공격·피격·대시 행동 중에는 판단을 쉬고, 행동이 끝나면(OnActionEnded) 다음 상태·콤보를 정한다.
// 상태 클래스는 State/ 폴더(상태마다 파일 하나)

public class AIFeature : BrainFeature
{
    private readonly MonsterController m_Controller;
    private readonly StateMachine<EAIState> m_Fsm = new();

    public MonsterController Controller => m_Controller;
    public EAIState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;

    public Vector3 HomePosition { get; private set; }           // 스폰 자리. 배회 기준
    public Vector3 LastHitDirection { get; private set; }

    // ------------------------------------------------

    public AIFeature(MonsterController controller) : base(controller)
    {
        m_Controller = controller;

        m_Fsm.Add(EAIState.Idle, new AIIdleState(this));
        m_Fsm.Add(EAIState.Wander, new AIWanderState(this));
        m_Fsm.Add(EAIState.Chase, new AIChaseState(this));
        m_Fsm.Add(EAIState.Flee, new AIFleeState(this));
        m_Fsm.Add(EAIState.Dead, new AIDeadState(this));
    }

    public override void Reset()
    {
        base.Reset();
        HomePosition = Owner.transform.position;
        LastHitDirection = Vector3.zero;
        m_Fsm.Change(EAIState.Idle);
    }

    protected override void Think(float deltaTime)
    {
        Owner.TargetF.Refresh(m_Controller.Def.SightRange);

        if (IsActing())
            return;

        m_Fsm.Update(deltaTime);
    }

    public void Change(EAIState next)
    {
        m_Fsm.Change(next);
    }

    // 맞으면 때린 쪽을 노린다
    public override void OnHit(HitParams hit)
    {
        LastHitDirection = hit.Direction;

        Owner.TargetF.SetTarget(hit.Attacker);
    }

    public override void OnDied()
    {
        m_Fsm.Change(EAIState.Dead);
    }

    // 공격은 끝날 때마다 쿨을 걸고, 끝까지 했으면 콤보 다음 타. 피격 경직이 풀리면 도망 또는 추적
    protected override void OnActionEnded(EActionType action, bool isFinished)
    {
        if (Owner.IsDead)
            return;

        AttackChoiceFeature attackChoice = m_Controller.AttackChoiceF;
        switch (action)
        {
            case EActionType.Attack:
                attackChoice.StartCooldown();
                AttackSpec comboNext = Owner.ActionLayer.ComboNext;
                if (isFinished && comboNext != null)
                    Owner.ActionLayer.RequestAttack(comboNext, Vector3.zero, Owner.TargetF.TargetTransform);

                break;
            case EActionType.Hit:
                if (isFinished)
                    m_Fsm.Change(m_Controller.Def.FleeTime > 0f ? EAIState.Flee : EAIState.Chase);

                break;
        }
    }

    // 진행 중인 상태를 Exit로 정리(애니 배속 복구)한다
    protected override void OnStop()
    {
        m_Fsm.Stop();
    }

    // ==================================================

    // 공격·피격·대시는 행동 층이 끝까지 맡는다. 사망은 사망 뒤 처리(드롭·사라짐)를 위해 판단을 계속 돈다
    private bool IsActing()
    {
        EActionType action = Owner.ActionLayer.Current;
        return action == EActionType.Attack || action == EActionType.Hit || action == EActionType.Dash;
    }
}
