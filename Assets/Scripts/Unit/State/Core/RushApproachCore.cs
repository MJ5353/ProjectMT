using UnityEngine;

// ==================================================

// 러시 돌진(플레이어). RushSub가 대상·수치를 넣고 요청한다. 대상 앞(베기 사거리 안)까지 달리고, 닿거나 막히거나(ApproachMaxTime) 대상이 죽으면 끝.
// 끝나면 기본 코어로 돌아가고, 빈틈 타이머는 RushSub가 이어서 돌린다. 공격 예약은 막고(도착한 뒤 낸다) 피격·사망에만 끊긴다

public class RushApproachCore : CoreState
{
    private UnitController m_Target;
    private float m_Reach;          // 대상 몸 표면부터 이 거리 안이면 도착(러시 좌 베기 Reach)
    private float m_Speed;          // RushSpec.ApproachSpeed
    private float m_MaxTime;        // RushSpec.ApproachMaxTime
    private Vector3 m_FaceDirection; // 달리는 쪽. 다음 프레임 Update에서 TurnSpeed로 돈다(MoveCore와 같은 순서)
    private bool m_IsDone;

    public override ECoreState Type => ECoreState.Approach;
    public override bool IsFinished => m_IsDone;

    // ------------------------------------------------

    public RushApproachCore(UnitController owner) : base(owner) { }

    // 대상 쪽 수평 거리
    public static Vector3 GetOffset(UnitController owner, UnitController target)
    {
        Vector3 offset = target.transform.position - owner.transform.position;
        offset.y = 0f;
        return offset;
    }

    // 대상 몸 반지름 + reach 안인지
    public static bool IsInReach(UnitController owner, UnitController target, float reach)
    {
        float stopDistance = target.HurtBody.radius * target.transform.lossyScale.x + reach;
        return GetOffset(owner, target).magnitude <= stopDistance;
    }

    public void Setup(UnitController target, float reach, float speed, float maxTime)
    {
        m_Target = target;
        m_Reach = reach;
        m_Speed = speed;
        m_MaxTime = maxTime;
    }

    public override bool CanInterruptBy(ECoreState next)
    {
        return next >= ECoreState.Hit;
    }

    // 요청한 프레임부터 달린다
    protected override void Enter()
    {
        m_IsDone = false;
        Run();
    }

    protected override void Update(float deltaTime)
    {
        Owner.MoveF.FaceTo(m_FaceDirection, deltaTime);

        // 대상이 죽으면 그 자리에서 러시 시작(RushSub가 같은 프레임에 본다)
        if (m_Target.IsValid() == false)
        {
            m_IsDone = true;
            return;
        }

        if (IsInReach(Owner, m_Target, m_Reach) || Time >= m_MaxTime)
        {
            Owner.MoveF.FaceToInstant(GetOffset(Owner, m_Target));
            m_IsDone = true;
            return;
        }

        Run();
    }

    public override void Exit()
    {
        Owner.MoveF.Stop();
    }

    // ==================================================

    private void Run()
    {
        Vector3 velocity = GetOffset(Owner, m_Target).normalized * m_Speed;
        m_FaceDirection = velocity;
        
        Owner.MoveF.SetVelocity(velocity);
        Owner.AnimF.Play(AnimKey.Run);
    }
}
