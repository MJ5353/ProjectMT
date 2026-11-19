// ==================================================

// 피격 경직. 멈춰서 Hit 애니를 틀고 경직 시간이 지나면 끝. 경직 중 또 맞으면 처음부터

public class HitAction : UnitAction
{
    private readonly float m_StunTime;   // UnitDef.HitStunTime

    public override EActionType Type => EActionType.Hit;
    public override bool IsFinished => Time >= m_StunTime;

    // ------------------------------------------------

    public HitAction(UnitController owner, float stunTime) : base(owner)
    {
        m_StunTime = stunTime;
    }

    public override bool CanInterruptBy(EActionType next)
    {
        return next >= EActionType.Hit;
    }

    protected override void Enter()
    {
        Owner.MoveF.Stop();
        Owner.AnimF.Play("Hit", true);
    }
}
