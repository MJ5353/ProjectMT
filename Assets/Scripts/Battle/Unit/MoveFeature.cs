using UnityEngine;

// ==================================================

// CharacterController 이동. 주인이 원하는 수평 속도를 넣고, 넉백 같은 순간 속도는 따로 더해 감쇠시킨다.
// 주인의 판단(입력·AI)이 정한 속도로 움직여야 해서 LateTick에서 이동한다
public class MoveFeature : UnitFeature
{
    private const float MaxDeltaTime = 1f / 20f;
    private const float Gravity = -25f;
    private const float GroundedSpeed = -2f; // 땅에 붙어 있도록 누르는 속도
    private const float ImpulseDamping = 9f; // 넉백이 줄어드는 빠르기
    private const float ActorContactMargin = 0.2f; // [mj] todo) 임시값. 다른 캐릭터 캡슐과 이 거리 안이면 계단 오르기를 끈다

    // readonly
    private readonly CharacterController m_Controller;
    private readonly Transform m_Transform;
    private readonly float m_StepOffset; // 프리팹의 계단 높이
    private readonly float m_TurnSpeed; // 초당 회전 각도(UnitDef.TurnSpeed)

    private Vector3 m_Desired;
    private Vector3 m_Impulse;
    private float m_VerticalSpeed;

    public bool IsKnockedBack => m_Impulse.sqrMagnitude > 0.5f;
    public Vector3 Velocity => m_Desired + m_Impulse;

    // ------------------------------------------------

    public MoveFeature(IActor owner, CharacterController controller, float turnSpeed) : base(owner)
    {
        m_Controller = controller;
        m_Transform = owner.transform;
        m_StepOffset = controller.stepOffset;
        m_TurnSpeed = turnSpeed;
    }

    // 풀에서 다시 꺼낼 때. 속도를 비우고 충돌을 다시 켠다
    public override void Reset()
    {
        m_Desired = Vector3.zero;
        m_Impulse = Vector3.zero;
        m_VerticalSpeed = 0f;
        m_Controller.enabled = true;
    }

    public override void LateTick(float deltaTime)
    {
        if (m_Controller.enabled == false)
            return;

        // 씬 로드 직후 같은 긴 프레임에 한 번에 크게 떨어지지 않게 자른다
        deltaTime = Mathf.Min(deltaTime, MaxDeltaTime);
        if (deltaTime <= 0f)
            return;

        if (m_Controller.isGrounded && m_VerticalSpeed < 0f)
            m_VerticalSpeed = GroundedSpeed;

        m_VerticalSpeed += Gravity * deltaTime;

        Vector3 motion = (m_Desired + m_Impulse) * deltaTime;
        motion.y = m_VerticalSpeed * deltaTime;

        // !!) 계단 오르기가 켜져 있으면 둥근 캡슐 바닥이 다른 캐릭터를 계단으로 보고 타고 올라간다(큰 몬스터가 플레이어 위로)
        ActorService actors = GameManager.Instance?.ActorS;
        bool isTouchingActor = actors != null && actors.IsBodyNear(m_Controller, ActorContactMargin);

        m_Controller.stepOffset = isTouchingActor ? 0f : m_StepOffset;
        m_Controller.Move(motion);
        m_Impulse = Vector3.Lerp(m_Impulse, Vector3.zero, 1f - Mathf.Exp(-ImpulseDamping * deltaTime));
    }

    // 풀로 돌아갈 때 멈춘다
    public override void Clear()
    {
        Stop();
    }

    public void Stop()
    {
        m_Desired = Vector3.zero;
    }
    
    public void SetVelocity(Vector3 velocity)
    {
        velocity.y = 0f;
        m_Desired = velocity;
    }

    public void AddImpulse(Vector3 impulse)
    {
        impulse.y = 0f;
        m_Impulse += impulse;
    }

    public void AddVerticalSpeed(float speed)
    {
        m_VerticalSpeed = Mathf.Max(m_VerticalSpeed, 0f) + speed;
    }

    public void FaceTo(Vector3 direction, bool instant = false)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion target = Quaternion.LookRotation(direction);
        m_Transform.rotation = instant ? target : Quaternion.RotateTowards(m_Transform.rotation, target, m_TurnSpeed * Time.deltaTime);
    }

    public void SetCollision(bool isOn)
    {
        m_Controller.enabled = isOn;
    }
}