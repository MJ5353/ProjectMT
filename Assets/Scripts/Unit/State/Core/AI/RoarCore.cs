using UnityEngine;

// ==================================================

// 포효(Boss: 첫 발견·분노, BossWatchSub가 끼운다). 멈춰서 대상을 보며 Roar 애니를 한 번 틀고 추적으로 간다. 피격에 끊기지 않는다(사망만).
// 시작할 때 카메라를 흔들고 BossRoared를 알린다(HUD가 붉은 가장자리·이름 배너)

public class RoarCore : AICore
{
    private const string RoarClip = "Roar";

    private readonly BossWatchSub m_Watch;
    private float m_Length;

    public override ECoreState Type => ECoreState.Roar;

    // ------------------------------------------------

    public RoarCore(UnitController owner, AIDef def, TargetSenseSub sense, BossWatchSub watch) : base(owner, def, sense)
    {
        m_Watch = watch;
    }

    public override bool CanInterruptBy(ECoreState next)
    {
        return next == ECoreState.Dead;
    }

    protected override void Enter()
    {
        base.Enter();
        Move(Vector3.zero, 0f);
        m_Length = Owner.AnimF.GetLength(RoarClip);
        Owner.AnimF.Play(RoarClip, true);

        DefineDef define = GameManager.Instance.Define;
        CameraManager.Instance.Shake(define.Camera.RoarShake, define.Camera.RoarShakeTime);
        SEventManager.Publish(new BossRoared(Owner, m_Watch.IsEnraged));
    }

    protected override void Decide(float deltaTime)
    {
        Owner.MoveF.FaceTo(Target.GetDirection(), deltaTime);
        if (TimeInState >= m_Length)
            ChangeCore(ECoreState.Chase);
    }
}
