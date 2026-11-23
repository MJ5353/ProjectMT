using UnityEngine;

// ==================================================

// 이동·대기. 다른 행동이 끝나면 여기로 돌아온다. 판단 쪽이 SetMove로 속도를 줄 때만 움직인다(스스로 아무것도 하지 않음)

public class MoveCore : CoreState
{
    private const float StopSpeed = 0.01f;

    private Vector3 m_FaceDirection; // 걷는 쪽. 0이면 돌지 않는다(대기)

    public override ECoreState Type => ECoreState.Move;

    // ------------------------------------------------

    public MoveCore(UnitController owner) : base(owner) { }

    // 속도가 거의 0이면 멈추고 Idle, 아니면 그쪽을 보며 걷기·달리기(도는 건 Update에서 유닛 시간으로)
    public void SetMove(Vector3 velocity, bool isRun)
    {
        velocity.y = 0f;
        if (velocity.sqrMagnitude < StopSpeed)
        {
            m_FaceDirection = Vector3.zero;
            Owner.MoveF.Stop();
            Owner.AnimF.Play(AnimKey.Idle);
            return;
        }

        m_FaceDirection = velocity;
        Owner.MoveF.SetVelocity(velocity);
        Owner.AnimF.Play(isRun ? AnimKey.Run : AnimKey.Walk);
    }

    // virtual ---

    protected override void Enter()
    {
        m_FaceDirection = Vector3.zero;
    }

    protected override void Update(float deltaTime)
    {
        Owner.MoveF.FaceTo(m_FaceDirection, deltaTime);
    }
}
