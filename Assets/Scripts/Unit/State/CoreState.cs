// ==================================================

// 코어 상태 종류. 숫자가 우선순위다(높을수록 지금 코어를 끊기 쉽다)
// Move~Down은 기본 층(이동·판단): 서로는 끊기 규칙 없이 바뀌고(AI 코어는 StateFeature.ChangeCore), 행동(Approach~)이 끝나면 마지막 기본 층 코어로 돌아간다
public enum ECoreState
{
    Move = 0,       // 플레이어 자유 이동(FreeCore)
    Idle,           // 이하 AI 코어(판단 + 이동)
    Wander,
    Chase,
    Flee,
    Follow,         // 주인 따라가기(Pet)
    Burrow,         // 땅속에 숨었다 나오기(Burrow)
    Roar,           // 포효(Boss). 피격에 끊기지 않는다
    Down,           // 쓰러짐(Boss). 피격에 끊기지 않는다
    Approach,       // 러시 돌진(플레이어). 공격 예약을 막고 피격·사망에만 끊긴다
    Attack,
    Dash,
    Hit,
    Dead,
}

// ==================================================

// 코어 상태: 유닛 몸을 차지하는 일 하나(이동·공격·대시·피격·사망). StateFeature가 한 번에 하나만 돌린다.
// 무엇을 할지는 판단 쪽(입력·AI)이 요청하고, 코어는 자기 시간·끝·끊길 수 있는지만 안다.
// 다른 부품(Move·Anim 등)에서 바꾼 것은 자기 Exit에서 되돌린다

public abstract class CoreState
{
    protected UnitController Owner { get; }

    public abstract ECoreState Type { get; }
    public float Time { get; private set; }         // 코어를 시작하고 지난 시간
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

    // 기본: 우선순위가 더 높은 코어만 끊을 수 있다
    public virtual bool CanInterruptBy(ECoreState next)
    {
        return next > Type;
    }

    // virtual ---

    protected virtual void Enter() { }
    protected virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
    public virtual void Think(float deltaTime) { }  // 판단(AI 코어). 서브·요청 정리 다음에 돈다. 멈춤·시간 정지(히트스톱) 중에는 쉰다
}
