// ==================================================

// 서브 상태 수명. Unit은 Def가 등록해 유닛과 함께 살고, Timed는 끝나면(IsFinished) 스스로 빠진다
public enum ESubLifetime
{
    Unit = 0,
    Timed = 1,
}

// ==================================================

// 서브 상태: 코어와 함께 도는 일(입력 읽기·러시 등). 여러 개가 겹쳐 돌 수 있다. StateFeature가 코어보다 먼저 Tick한다.
// 빠질 때(RemoveSub·Stop·풀 반환)는 반드시 Exit를 부른다. 알림(피격·사망·코어 바뀜·퍼펙트 닷지)은 구독 대신 아래 훅으로 받는다

public abstract class SubState
{
    protected UnitController Owner { get; }

    public abstract ESubLifetime Lifetime { get; }
    public float Time { get; private set; }         // 시작하고 지난 시간
    public virtual bool IsFinished => false;        // Timed만 본다. 끝나면 StateFeature가 Exit하고 뺀다

    // ------------------------------------------------

    protected SubState(UnitController owner)
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
        Tick(deltaTime);
    }

    // virtual ---

    protected virtual void Enter() { }
    protected virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }

    public virtual void OnHit(in HitParams hit) { }                           // 맞았을 때(살아 있을 때만)
    public virtual void OnDied() { }                                          // 죽었을 때
    public virtual void OnPerfectDodge(in HitParams hit) { }                  // 퍼펙트 닷지
    public virtual void OnCoreChanged(ECoreState prev, ECoreState next) { }   // 코어가 바뀌었을 때(새 코어 Begin 직후)
}
