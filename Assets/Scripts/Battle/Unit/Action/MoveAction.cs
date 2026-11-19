using UnityEngine;

// ==================================================

// 이동·대기. 다른 행동이 끝나면 여기로 돌아온다. 판단 쪽이 SetMove로 속도를 줄 때만 움직인다(스스로 아무것도 하지 않음)

public class MoveAction : UnitAction
{
    private const float StopSpeed = 0.01f;

    public override EActionType Type => EActionType.Move;

    // ------------------------------------------------

    public MoveAction(UnitController owner) : base(owner) { }

    // 속도가 거의 0이면 멈추고 Idle, 아니면 그쪽을 보며 걷기·달리기
    public void SetMove(Vector3 velocity, bool isRun)
    {
        velocity.y = 0f;
        if (velocity.sqrMagnitude < StopSpeed)
        {
            Owner.Move.Stop();
            Owner.Anim.Play("Idle");
            return;
        }

        Owner.Move.SetVelocity(velocity);
        Owner.Move.FaceTo(velocity);
        Owner.Anim.Play(isRun ? "Run" : "Walk");
    }
}
