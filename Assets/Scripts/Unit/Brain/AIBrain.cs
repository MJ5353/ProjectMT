using UnityEngine;

// ==================================================

// 판단 상태. 행동(공격·피격 등)은 실행기(UnitExecutor, EActionType)이 맡고, 여기는 "무엇을 할지"만
public enum EAIState
{
    Idle,
    Wander,
    Chase,
    Flee,
    Hide,       // 땅속에 숨었다 나오기(Burrow)
    Roar,       // 포효(Boss: 첫 발견·분노)
    Down,       // 부위가 깨져 쓰러짐(Boss)
    Follow,     // 주인 따라가기(Pet)
    Dead,
}

// ==================================================

// 공통 Idle·Wander·Chase·Dead
//  | Normal +Flee
//  | Burrow +Hide
//  | Boss +Roar·Down
//  | Pet은 Follow·Dead만

// AI 판단(몬스터·보스·동료 공통). 대상(TargetSelector)·공격 고르기(AttackSelector)·상태 머신·스폰 자리를 갖고 실행기(UnitExecutor)에 요청만 한다.
// 종류 차이는 클래스가 아니라 AI 프리셋(AIDef.Type)에 따라 등록하는 상태 묶음으로 낸다(SetStates). 상태는 State/ 폴더, 수치는 AIDef.
// 공격·피격·대시 행동 중에는 상태를 돌리지 않고, 감지(대상·쿨·보스 분노)만 계속한다

public class AIBrain : BrainBase
{
    private readonly UnitDef m_UnitDef;
    private readonly StateMachine<EAIState> m_Fsm = new();
    private UnitController m_Leader;

    public AIDef Def { get; }
    public AttackSelector AttackSelector { get; }                   // 공격 고르기·쿨
    public Vector3 HomePosition { get; private set; }           // 스폰 자리. 배회 기준
    public Vector3 LastHitDirection { get; private set; }
    public bool IsEnraged { get; private set; }

    // boss
    private bool m_IsRoarPending;   // 다음에 쫓을 때 포효
    private bool m_IsDownPending;   // 행동(피격 경직)이 끝나면 쓰러짐

    public UnitController Controller => Owner;
    public float MoveSpeed => m_UnitDef.MoveSpeed;                       // 추적·땅속 이동·따라가기
    public float WanderSpeed => m_UnitDef.MoveSpeed * Def.WanderSpeedRate;
    public EAIState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;
    public override bool CanFlinch => State != EAIState.Down && State != EAIState.Roar; // 쓰러짐·포효는 맞아도 끊기지 않는다

    // ------------------------------------------------

    public AIBrain(UnitController owner, UnitDef unitDef) : base(owner)
    {
        m_UnitDef = unitDef;
        Def = unitDef.AI;
        
        if (Def == null)
        {
            CLogger.Error("AIBrain", $"{unitDef.name}의 AI 프리셋(UnitDef.AI)이 비어 있어 기본값으로 움직입니다", owner);
            Def = ScriptableObject.CreateInstance<AIDef>();
        }

        AttackSelector = new AttackSelector(unitDef, Def.AttackCooldown);
        SetStates(Def.Type);

        // 부위는 판단보다 먼저 붙는다(MonsterDef.SetFeatures)
        if (Def.Type == EAIType.Boss && owner.PartF != null)
            owner.PartF.Broken += OnPartBroken;
    }

    public override void Reset()
    {
        base.Reset();
        AttackSelector.Reset();

        m_IsRoarPending = Def.Type == EAIType.Boss;
        m_IsDownPending = false;
        IsEnraged = false;
        HomePosition = Owner.transform.position;
        LastHitDirection = Vector3.zero;

        m_Fsm.Change(Def.Type == EAIType.Pet ? EAIState.Follow : EAIState.Idle);
    }

    // 감지는 행동 중에도, 상태는 행동이 없을 때만 돌린다
    protected override void Think(float deltaTime)
    {
        AttackSelector.Tick(deltaTime);
        
        if (Def.Type != EAIType.Pet)
            TargetSelector.Refresh(Def.SightRange);
        
        if (Def.Type == EAIType.Boss)
            UpdateBoss();

        if (IsActing())
            return;

        m_Fsm.Update(deltaTime);
    }

    public void Change(EAIState next)
    {
        m_Fsm.Change(next);
    }

    // 종류별 상태 묶음. 상태는 어느 종류에나 끼울 수 있다
    private void SetStates(EAIType type)
    {
        m_Fsm.Add(EAIState.Dead, new AIDeadStateBase(this));
        if (type == EAIType.Pet)
        {
            m_Fsm.Add(EAIState.Follow, new AIFollowStateBase(this));
            return;
        }

        m_Fsm.Add(EAIState.Idle, new AIIdleStateBase(this));
        m_Fsm.Add(EAIState.Wander, new AIWanderStateBase(this));
        m_Fsm.Add(EAIState.Chase, new AIChaseStateBase(this));
        switch (type)
        {
            case EAIType.Burrow:
                m_Fsm.Add(EAIState.Hide, new AIHideStateBase(this));
                break;
            case EAIType.Boss:
                m_Fsm.Add(EAIState.Roar, new AIRoarStateBase(this));
                m_Fsm.Add(EAIState.Down, new AIDownStateBase(this));
                break;
            default:
                m_Fsm.Add(EAIState.Flee, new AIFleeStateBase(this));
                break;
        }
    }
    
    #region OnAction
    
    // 맞으면 때린 쪽을 노린다
    public override void OnHit(HitParams hit)
    {
        LastHitDirection = hit.Direction;
        TargetSelector.SetTarget(hit.Attacker);
    }

    public override void OnDied()
    {
        m_Fsm.Change(EAIState.Dead);
    }

    // 공격은 끝날 때마다 쿨을 걸고, 끝까지 했으면 콤보 다음 타. 피격 경직이 풀리면 종류별 다음 상태(쓰러짐·포효 중엔 경직하지 않는다)
    protected override void OnActionEnded(EActionType action, bool isFinished)
    {
        if (Owner.IsDead)
            return;

        switch (action)
        {
            case EActionType.Attack:
            {
                AttackSelector.StartCooldown();
                AttackSpec comboNext = Owner.UnitExecutor.ComboNext;

                if (isFinished && comboNext != null)
                    Owner.UnitExecutor.RequestAttack(comboNext, Vector3.zero, TargetSelector.TargetTransform);

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
    
    #endregion    
    
    // pet ---

    public void SetLeader(UnitController leader)
    {
        m_Leader = leader;
    }

    // boss ---

    // 분노 확인 뒤, 행동이 없으면 쓰러짐 > 포효 순으로 끼운다
    private void UpdateBoss()
    {
        if (Owner.IsDead)
            return;

        UpdateEnrage();
        if (IsActing())
            return;

        if (m_IsDownPending)
        {
            m_IsDownPending = false;
            m_Fsm.Change(EAIState.Down);
            return;
        }

        if (m_IsRoarPending && State == EAIState.Chase)
        {
            m_IsRoarPending = false;
            m_Fsm.Change(EAIState.Roar);
        }
    }

    // 체력이 Def.EnrageHpRate 이하가 되면 공용 쿨을 줄이고 분노 스킬을 더한 뒤 포효
    private void UpdateEnrage()
    {
        if (IsEnraged || Def.EnrageHpRate <= 0f)
            return;

        HealthFeature health = Owner.HealthF;
        if (health.Current > health.MaxHp * Def.EnrageHpRate)
            return;

        IsEnraged = true;
        AttackSelector.Enrage(Def.ListEnrageSkill, Def.EnrageCooldownRate);
        m_IsRoarPending = true;
    }

    private void OnPartBroken(PartFeature.Part part)
    {
        if (Owner.IsDead == false)
            m_IsDownPending = true;
    }
    
    // get ---
    
    // 공격·피격·대시는 실행기가 끝까지 맡는다. 사망은 사망 뒤 처리(사라짐)를 위해 판단을 계속 돈다
    private bool IsActing()
    {
        EActionType action = Owner.UnitExecutor.Current;
        return action == EActionType.Attack || action == EActionType.Hit || action == EActionType.Dash;
    }

    private EAIState GetStateAfterHit()
    {
        switch (Def.Type)
        {
            case EAIType.Burrow:
                return EAIState.Hide;
            case EAIType.Pet:
                return EAIState.Follow;
            case EAIType.Normal:
                return Def.FleeTime > 0f ? EAIState.Flee : EAIState.Chase;
            default:
                return EAIState.Chase;
        }
    }

    // 따라갈 주인. 정한 주인이 없거나 죽었으면 플레이어
    public UnitController GetLeader()
    {
        if (m_Leader != null && m_Leader.IsDead == false)
            return m_Leader;

        return GameManager.Instance?.UnitS?.Player;
    }
}
