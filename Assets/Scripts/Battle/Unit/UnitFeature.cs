
// ==================================================

// 유닛 부품(Move·Anim·Health·Hit 등)의 기반. 주인(UnitController)이 만들어 등록하고 수명 주기를 한 번에 돌린다.
// 순서: 풀에서 꺼낼 때 Reset → 매 프레임 Tick(주인 판단 전) → 주인 판단 → LateTick(주인 판단 후) → 풀로 돌아갈 때 Clear

public abstract class UnitFeature
{
    protected IActor Owner { get; }

    // 히트스톱(timeScale 0) 중에도 돌아야 하면 true. 주인이 unscaled 시간으로 Tick·LateTick을 부른다
    public virtual bool IsUnscaledTime => false;

    // ------------------------------------------------

    protected UnitFeature(IActor owner)
    {
        Owner = owner;
    }

    // virtual ---

    public virtual void Reset() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void LateTick(float deltaTime) { }
    public virtual void Clear() { }
}
