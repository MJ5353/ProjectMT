
// ==================================================

// 판단 층의 기반(플레이어 입력·몬스터 AI). "무엇을 할지"만 정해 행동 층(ActionLayer)에 요청한다.
// 주인이 부품 목록과 따로 들고, 매 프레임 부품 Tick → 행동 다음, 이동(LateTick) 전에 부른다.
// 전투가 끝나면 스스로 멈추고(OnStop) 하던 행동을 정리한다. 피격·사망·행동 끝은 훅으로 받는다

public abstract class BrainFeature : UnitFeature
{
    public bool IsStopped { get; private set; }

    // ------------------------------------------------

    protected BrainFeature(UnitController owner) : base(owner)
    {
        // 행동 층은 판단보다 먼저 만들어진다(UnitController.Set)
        Owner.ActionLayer.Ended += OnActionLayerEndedInternal;
    }

    public override void Reset()
    {
        IsStopped = false;
    }

    // 전투 중일 때만 판단한다. 승패가 난 프레임에 한 번 멈춘다
    public sealed override void Tick(float deltaTime)
    {
        if (IsStopped)
            return;

        BattleService battle = GameManager.Instance?.BattleS;
        if (battle != null && battle.State != EBattleState.Playing)
        {
            Stop();
            return;
        }

        Think(deltaTime);
    }

    // 하던 판단·행동을 정리하고 제자리에 세운다. 중력만 계속 받는다
    public void Stop()
    {
        if (IsStopped)
            return;

        IsStopped = true;
        OnStop();

        Owner.ActionLayer.Interrupt();
        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);
        if (Owner.IsDead == false)
            Owner.AnimF.Play(AnimKey.Idle);
    }

    // ==================================================

    private void OnActionLayerEndedInternal(EActionType action, bool isFinished)
    {
        if (IsStopped)
            return;

        OnActionEnded(action, isFinished);
    }

    // virtual ---

    protected abstract void Think(float deltaTime);                      // 매 프레임 판단. 행동 층에 요청만
    public virtual void OnHit(HitParams hit) { }                         // 맞았을 때(살아 있을 때만). 피격 행동 요청은 주인이 한다
    public virtual void OnDied() { }                                     // 죽었을 때. 사망 행동 요청은 주인이 한다
    protected virtual void OnActionEnded(EActionType action, bool isFinished) { } // 행동이 끝났을 때 다음 판단
    protected virtual void OnStop() { }                                  // 전투가 끝나 멈출 때
}
