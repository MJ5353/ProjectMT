// ==================================================

// 사망. 멈춰서 Death 애니를 튼다. 끝나지 않고 무엇에도 끊기지 않는다(그 뒤 처리는 주인이 이벤트로)

public class DeadAction : UnitAction
{
    public override EActionType Type => EActionType.Dead;

    // ------------------------------------------------

    public DeadAction(UnitController owner) : base(owner) { }

    public override bool CanInterruptBy(EActionType next)
    {
        return false;
    }

    protected override void Enter()
    {
        Owner.Move.Stop();
        Owner.Anim.Play("Death", true);
    }
}
