// ==================================================

// 행동 종류. 숫자가 우선순위다(높을수록 지금 행동을 끊기 쉽다)
public enum EActionType
{
    Move = 0,
    Attack = 1,
    Dash = 2,
    Hit = 3,
    Dead = 4,
}

// ==================================================

// 유닛이 지금 하고 있는 행동 하나(이동·공격·대시·피격·사망). ActionLayer가 하나씩 돌린다.
// 무엇을 할지는 판단 쪽(입력·AI)이 요청하고, 행동은 자기 시간·끝·끊길 수 있는지만 안다

public abstract class UnitAction
{
    protected UnitController Owner { get; }

    public abstract EActionType Type { get; }
    public float Time { get; private set; }         // 행동을 시작하고 지난 시간
    public virtual bool IsFinished => false;        // 끝나면 ActionLayer가 이동(대기)으로 돌린다

    // ------------------------------------------------

    protected UnitAction(UnitController owner)
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

    // 기본: 우선순위가 더 높은 행동만 끊을 수 있다
    public virtual bool CanInterruptBy(EActionType next)
    {
        return next > Type;
    }

    // virtual ---

    protected virtual void Enter() { }
    protected virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}
