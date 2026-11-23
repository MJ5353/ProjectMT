
// ==================================================

// 판단(UnitBrain)의 기반(플레이어 입력·몬스터 AI). "무엇을 할지"만 정해 실행기(UnitExecutor)에 요청한다.
// Feature(부품)가 아니라 주인이 직접 들고, 매 프레임 부품 Tick → 행동 다음, 이동(LateTick) 전에 부른다.
// 감지(대상 찾기·거리)는 TargetSelector가 맡는다.
// 시작 팝업·시장·결과에서는 BattleFlow가 멈추고(UnitService.StopBrains → Stop) 하던 행동을 정리한다. 시작 팝업을 누르면 다시 켠다(ResumeBrains → Resume). 피격·사망·행동 끝은 훅으로 받는다

public abstract class BrainBase
{
    protected UnitController Owner { get; }
    public TargetSelector TargetSelector { get; }     // 감지: 노리는 적·가장 가까운 적·거리·조준
    public bool IsStopped { get; private set; }

    // ------------------------------------------------

    protected BrainBase(UnitController owner)
    {
        Owner = owner;
        TargetSelector = new TargetSelector(owner);

        // 실행기는 판단보다 먼저 만들어진다(UnitController.Set)
        Owner.UnitExecutor.Ended += OnExecutorEndedInternal;
    }

    // 풀에서 꺼낼 때
    public virtual void Reset()
    {
        IsStopped = false;
        TargetSelector.Reset();
    }

    // 풀로 돌아갈 때
    public virtual void Clear()
    {
        TargetSelector.Clear();
    }

    // 멈추기 전까지 판단한다
    public void Tick(float deltaTime)
    {
        if (IsStopped)
            return;

        Think(deltaTime);
    }

    // 하던 판단·행동을 정리하고 제자리에 세운다. 중력만 계속 받는다
    public void Stop()
    {
        if (IsStopped)
            return;

        IsStopped = true;
        OnStop();

        Owner.UnitExecutor.Interrupt();
        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);
        
        if (Owner.IsDead == false)
            Owner.AnimF.Play(AnimKey.Idle);
    }

    // 멈춘 판단을 처음 상태부터 다시 돌린다. 죽은 유닛은 멈춘 채 둔다(풀로 돌아가면 Reset이 푼다)
    public void Resume()
    {
        if (IsStopped == false || Owner.IsDead)
            return;

        IsStopped = false;
        OnResume();
    }

    // ==================================================

    private void OnExecutorEndedInternal(EActionType action, bool isFinished)
    {
        if (IsStopped)
            return;

        OnActionEnded(action, isFinished);
    }

    // virtual ---

    protected abstract void Think(float deltaTime);                      // 매 프레임 판단. 실행기에 요청만
    
    public virtual void OnHit(HitParams hit) { }                         // 맞았을 때(살아 있을 때만). 피격 행동 요청은 주인이 한다
    public virtual void OnDied() { }                                     // 죽었을 때. 사망 행동 요청은 주인이 한다
    public virtual void OnPerfectDodge(HitParams hit) { }                // 퍼펙트 닷지(판정 직전 범위 안에 있다가 대시로 안 맞음, HitFeature.PerfectDodged)
    public virtual bool CanFlinch => true;                               // false면 맞아도 경직(피격 행동·넉백) 없이 데미지만

    protected virtual void OnActionEnded(EActionType action, bool isFinished) { } // 행동이 끝났을 때 다음 판단
    protected virtual void OnStop() { }                                  // 멈출 때(시작 팝업·시장·결과)
    protected virtual void OnResume() { }                                // 다시 켤 때(시작 팝업을 누름)
}
