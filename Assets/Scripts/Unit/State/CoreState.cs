// ==================================================

// 코어 상태 종류. 숫자가 우선순위다(높을수록 지금 코어를 끊기 쉽다)
public enum ECoreState
{
    Move = 0,
    Attack = 1,
    Dash = 2,
    Hit = 3,
    Dead = 4,
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
    public virtual bool IsFinished => false;        // 끝나면 StateFeature가 이동(대기)으로 돌린다

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
}
