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

// AI 상태가 읽는 수치. Def가 이 인터페이스를 이어받는다(MonsterDef). 데이터는 Def에 그대로 둔다
public interface IAIDef
{
    float MoveSpeed { get; }        // 추적 속도
    float WanderSpeed { get; }      // 배회 속도
    float SightRange { get; }       // 적을 알아채는 거리
    float LoseRange { get; }        // 추적을 포기하는 거리
    float WanderRadius { get; }     // 처음 자리에서 배회하는 반경
    float AttackRange { get; }      // 추적을 멈추고 마주 보는 거리
    float AttackCooldown { get; }   // 공격 하나가 끝난 뒤 공용 쿨(초)
    float FleeTime { get; }         // 맞은 뒤 도망치는 시간. 0이면 바로 반격
    float FleeSpeed { get; }
}

// ==================================================

// AI 판단의 기반. 대상(Target)·공격 고르기(AttackChoice)·스폰 자리·AI 상태 머신을 갖고, 행동 층(ActionLayer)에 요청만 한다.
// AI 상태(State/)는 이 클래스와 UnitController·IAIDef만 보므로 몬스터·보스·동료 판단이 같은 상태를 다시 쓴다.
// 다른 동작이 필요하면 이어받은 판단이 생성자에서 SetState로 상태를 바꿔 끼우거나 훅(OnDeadStart 등)을 덮는다.
// 공격·피격·대시 행동 중에는 판단을 쉬고, 행동이 끝나면(OnActionEnded) 다음 상태·콤보를 정한다

public abstract class AIBrain : BrainLayer
{
    private readonly StateMachine<EAIState> m_Fsm = new();

    public UnitController Controller => Owner;
    public IAIDef Def { get; }
    public AttackChoice AttackChoice { get; }                   // 공격 고르기·쿨
    public Vector3 HomePosition { get; private set; }           // 스폰 자리. 배회 기준
    public Vector3 LastHitDirection { get; private set; }

    public EAIState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;

    // ------------------------------------------------

    protected AIBrain(UnitController owner, UnitDef unitDef, IAIDef aiDef) : base(owner)
    {
        Def = aiDef;
        AttackChoice = new AttackChoice(unitDef, aiDef.AttackCooldown);

        SetState(EAIState.Idle, new AIIdleState(this));
        SetState(EAIState.Wander, new AIWanderState(this));
        SetState(EAIState.Chase, new AIChaseState(this));
        SetState(EAIState.Flee, new AIFleeState(this));
        SetState(EAIState.Dead, new AIDeadState(this));
    }

    public override void Reset()
    {
        base.Reset();
        AttackChoice.Reset();

        HomePosition = Owner.transform.position;
        LastHitDirection = Vector3.zero;
        m_Fsm.Change(EAIState.Idle);
    }

    protected override void Think(float deltaTime)
    {
        AttackChoice.Tick(deltaTime);
        Target.Refresh(Def.SightRange);

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
        Target.SetTarget(hit.Attacker);
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

        switch (action)
        {
            case EActionType.Attack:
            {
                AttackChoice.StartCooldown();
                AttackSpec comboNext = Owner.ActionLayer.ComboNext;

                if (isFinished && comboNext != null)
                    Owner.ActionLayer.RequestAttack(comboNext, Vector3.zero, Target.TargetTransform);

                break;
            }
            case EActionType.Hit:
            {
                if (isFinished)
                    m_Fsm.Change(Def.FleeTime > 0f ? EAIState.Flee : EAIState.Chase);

                break;
            }
        }
    }

    // 진행 중인 상태를 Exit로 정리(애니 배속 복구)한다
    protected override void OnStop()
    {
        m_Fsm.Stop();
    }

    // 상태 등록·교체. 이어받은 판단은 생성자에서 기본 상태를 바꿔 끼운다
    protected void SetState(EAIState key, AIState state)
    {
        m_Fsm.Add(key, state);
    }

    // ==================================================

    // 공격·피격·대시는 행동 층이 끝까지 맡는다. 사망은 사망 뒤 처리(드롭·사라짐)를 위해 판단을 계속 돈다
    private bool IsActing()
    {
        EActionType action = Owner.ActionLayer.Current;
        return action == EActionType.Attack || action == EActionType.Hit || action == EActionType.Dash;
    }

    // virtual ---

    public virtual void OnDeadStart() { }   // 사망 상태에 들어갈 때(드롭·처치 알림 등 타입별 처리)
}
