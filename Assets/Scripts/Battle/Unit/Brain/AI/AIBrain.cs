using UnityEngine;

// ==================================================

// 판단 상태. 행동(공격·피격 등)은 행동 층(EActionType)이 맡고, 여기는 "무엇을 할지"만
public enum EAIState
{
    Idle,
    Wander,
    Chase,
    Flee,
    Hide,   // 땅속에 숨었다 나오기(BurrowBrain)
    Dead,
}

// ==================================================

// AI 판단의 기반. 대상(Target)·공격 고르기(AttackChoice)·스폰 자리·AI 상태 머신을 갖고, 행동 층(ActionLayer)에 요청만 한다.
// AI 상태(State/)는 이 클래스와 UnitController·MonsterDef만 보므로 이어받은 판단(MonsterBrain·BurrowBrain·보스)이 같은 상태를 다시 쓴다.
// 다른 동작이 필요하면 이어받은 판단이 생성자에서 SetState로 상태를 더하거나 바꿔 끼우고, 훅(GetStateAfterHit·OnDeadStart)을 덮는다.
// 공격·피격·대시 행동 중에는 판단을 쉬고, 행동이 끝나면(OnActionEnded) 다음 상태·콤보를 정한다

public abstract class AIBrain : BrainLayer
{
    private readonly StateMachine<EAIState> m_Fsm = new();

    public UnitController Controller => Owner;
    public MonsterDef Def { get; }
    public AttackChoice AttackChoice { get; }                   // 공격 고르기·쿨
    public Vector3 HomePosition { get; private set; }           // 스폰 자리. 배회 기준
    public Vector3 LastHitDirection { get; private set; }

    public EAIState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;

    // ------------------------------------------------

    protected AIBrain(UnitController owner, MonsterDef def) : base(owner)
    {
        Def = def;
        AttackChoice = new AttackChoice(def, def.AttackCooldown);

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
                    m_Fsm.Change(GetStateAfterHit());

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

    // 피격 경직이 풀린 뒤 갈 상태. 기본은 도망(FleeTime이 있으면) 또는 바로 추적
    protected virtual EAIState GetStateAfterHit()
    {
        return Def.FleeTime > 0f ? EAIState.Flee : EAIState.Chase;
    }

    public virtual void OnDeadStart() { }   // 사망 상태에 들어갈 때(드롭·처치 알림 등 타입별 처리)
}
