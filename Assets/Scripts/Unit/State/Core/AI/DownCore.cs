using UnityEngine;

// ==================================================

// 쓰러짐(Boss: 부위가 깨졌을 때, BossWatchSub가 끼운다). PartBreak 애니 뒤 Down 애니로 Def.DownTime 동안 누워 있다가 추적으로 간다.
// 그동안 공격하지 않고 피격에 끊기지 않아(사망만) 플레이어가 마음껏 칠 수 있는 틈이 된다

public class DownCore : AICore
{
    private const string PartBreakClip = "PartBreak";
    private const string DownClip = "Down";

    private float m_BreakLength;
    private bool m_IsDown;

    public override ECoreState Type => ECoreState.Down;

    // ------------------------------------------------

    public DownCore(UnitController owner, AIDef def, TargetSenseSub sense) : base(owner, def, sense) { }

    public override bool CanInterruptBy(ECoreState next)
    {
        return next == ECoreState.Dead;
    }

    protected override void Enter()
    {
        base.Enter();
        Move(Vector3.zero, 0f);
        m_BreakLength = Owner.AnimF.GetLength(PartBreakClip);
        m_IsDown = false;
        Owner.AnimF.Play(PartBreakClip, true);
    }

    protected override void Decide(float deltaTime)
    {
        if (m_IsDown == false && TimeInState >= m_BreakLength)
        {
            m_IsDown = true;
            Owner.AnimF.Play(DownClip, true);
        }

        // 누운 채 맞으면 피격 애니가 덮으므로 다시 튼다(같은 애니가 재생 중이면 무시)
        Owner.AnimF.Play(m_IsDown ? DownClip : PartBreakClip);

        if (TimeInState >= m_BreakLength + Def.DownTime)
            ChangeCore(ECoreState.Chase);
    }
}
