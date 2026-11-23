using System;

// ==================================================

// 코어 상태 이름. 숫자는 이름표일 뿐 순서·우선순위가 아니다(AIDef 에셋에 숫자로 저장된다). 새 값은 맨 뒤에만 붙이고, 순서를 바꾸거나 숫자를 다시 쓰지 않는다.
// 끊기 규칙은 코어마다 Kind·InterruptedBy로 정한다(CoreState)
public enum ECoreState
{
    Free = 0,       // 플레이어 자유 이동(FreeCore)
    Idle = 1,       // 이하 AI 코어(판단 + 이동)
    Wander = 2,
    Chase = 3,
    Flee = 4,
    Follow = 5,     // 주인 따라가기(Pet)
    Burrow = 6,     // 땅속에 숨었다 나오기(Burrow)
    Roar = 7,       // 포효(Boss). 사망에만 끊긴다
    Down = 8,       // 쓰러짐(Boss). 사망에만 끊긴다
    RushApproach = 9, // 러시 돌진(플레이어, RushApproachCore). 공격 예약을 막고 피격·사망에만 끊긴다
    Attack = 10,
    Dash = 11,
    Hit = 12,
    Dead = 13,
}

// 코어 종류(끊기 규칙용). 코어는 Kind 하나를 갖고, 자기를 끊을 수 있는 종류를 InterruptedBy에 모아 둔다
[Flags]
public enum ECoreKind
{
    None = 0,
    Mode = 1 << 0,      // 모드: 몸을 묶지 않고 이동 애니만 쓰며 다음 행동을 고르는 자리(Free·Idle~Follow). 행동이 끝나면 마지막 모드 코어로 돌아온다. AI 코어끼리는 끊기 규칙 없이 바뀐다(StateFeature.ChangeCore)
    Action = 1 << 1,    // 행동: 전용 클립이나 정해진 움직임으로 몸을 묶는 동작(공격·러시 돌진·숨기·포효·쓰러짐)
    Dash = 1 << 2,
    Hit = 1 << 3,
    Dead = 1 << 4,
}

// ==================================================

// 코어 상태: 유닛 몸을 차지하는 일 하나(이동·공격·대시·피격·사망). StateFeature가 한 번에 하나만 돌린다.
// 무엇을 할지는 판단 쪽(입력·AI)이 요청하고, 코어는 자기 시간·끝·끊길 수 있는지만 안다.
// 다른 부품(Move·Anim 등)에서 바꾼 것은 자기 Exit에서 되돌린다

public abstract class CoreState
{
    protected UnitController Owner { get; }
    public float Time { get; private set; }         // 코어를 시작하고 지난 시간
    public abstract ECoreState Type { get; }

    public virtual ECoreKind Kind => ECoreKind.Mode;
    public virtual ECoreKind InterruptedBy => ECoreKind.Action | ECoreKind.Dash | ECoreKind.Hit | ECoreKind.Dead; // 이 코어를 끊을 수 있는 종류. 모드는 행동·대시·피격·사망 모두에 끊긴다
    public virtual bool IsFinished => false;        // 끝나면 StateFeature가 기본 코어로 돌린다

    // ------------------------------------------------

    protected CoreState(UnitController owner)
    {
        Owner = owner;
    }

    public void Begin()
    {
        Time = 0f;
        Enter();
    }

    public void Step(float deltaTime)
    {
        Time += deltaTime;
        Update(deltaTime);
    }

    // 기본: next의 종류가 InterruptedBy에 있으면 끊을 수 있다
    public virtual bool CanInterruptBy(CoreState next)
    {
        return (InterruptedBy & next.Kind) != 0;
    }

    // virtual ---

    protected virtual void Enter() { }
    protected virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
    public virtual void Think(float deltaTime) { }  // 판단(AI 코어). 서브·요청 정리 다음에 돈다. 멈춤·시간 정지(히트스톱) 중에는 쉰다
}
