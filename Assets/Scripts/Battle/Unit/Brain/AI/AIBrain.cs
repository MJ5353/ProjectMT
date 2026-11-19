using UnityEngine;

// ==================================================

// 판단 상태. 행동(공격·피격 등)은 행동 층(EActionType)이 맡고, 여기는 "무엇을 할지"만
public enum EAIState
{
    Idle,
    Wander,
    Chase,
    Flee,
    Hide,       // 땅속에 숨었다 나오기(BurrowBrain)
    Roar,       // 포효(BossBrain: 첫 발견·분노)
    Down,       // 부위가 깨져 쓰러짐(BossBrain)
    Follow,     // 주인 따라가기(PetBrain)
    Dead,
}

// ==================================================

// AI 판단의 기반(몬스터·동료). 상태 머신·대상(Target)·스폰 자리만 갖고, 행동 층(ActionLayer)에 요청만 한다.
// 이어받은 판단이 생성자에서 SetState로 상태를 등록하고, 감지(Sense)·피격·행동 끝 훅을 덮는다.
//   AIBrain ─ MonsterBrain(공격 고르기·추적·도망·드롭) ─ BurrowBrain(숨기) / BossBrain(포효·쓰러짐·분노)
//           └ PetBrain(따라가기)
// 공격·피격·대시 행동 중에는 상태를 돌리지 않는다. 상태 클래스는 State/ 폴더(상태마다 파일 하나)

public abstract class AIBrain : BrainLayer
{
    private readonly StateMachine<EAIState> m_Fsm = new();
    public Vector3 HomePosition { get; private set; }           // 스폰 자리. 배회 기준
    public Vector3 LastHitDirection { get; private set; }

    public EAIState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;
    public UnitController Controller => Owner;
    protected virtual EAIState StartState => EAIState.Idle;     // 스폰할 때 들어갈 상태

    // ------------------------------------------------

    protected AIBrain(UnitController owner) : base(owner) { }

    public override void Reset()
    {
        base.Reset();
        
        HomePosition = Owner.transform.position;
        LastHitDirection = Vector3.zero;
        m_Fsm.Change(StartState);
    }

    // 감지는 행동 중에도, 상태는 행동이 없을 때만 돌린다
    protected override void Think(float deltaTime)
    {
        Sense(deltaTime);
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

    // Dead 상태가 없는 판단(동료)은 그대로 둔다
    public override void OnDied()
    {
        m_Fsm.Change(EAIState.Dead);
    }

    // 진행 중인 상태를 Exit로 정리(애니 배속 복구)한다
    protected override void OnStop()
    {
        m_Fsm.Stop();
    }

    // 상태 등록·교체. 이어받은 판단이 생성자에서 부른다
    protected void SetState(EAIState key, IState state)
    {
        m_Fsm.Add(key, state);
    }

    // 공격·피격·대시는 행동 층이 끝까지 맡는다. 사망은 사망 뒤 처리(드롭·사라짐)를 위해 판단을 계속 돈다
    protected bool IsActing()
    {
        EActionType action = Owner.ActionLayer.Current;
        return action == EActionType.Attack || action == EActionType.Hit || action == EActionType.Dash;
    }

    // virtual ---

    protected virtual void Sense(float deltaTime) { }   // 매 프레임 감지(행동 중에도). 대상 찾기·쿨
}
