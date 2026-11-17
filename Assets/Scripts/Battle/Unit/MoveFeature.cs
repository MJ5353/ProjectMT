using UnityEngine;

// CharacterController 이동. 주인이 원하는 수평 속도를 넣고, 넉백 같은 순간 속도는 따로 더해 감쇠시킨다.
[RequireComponent(typeof(CharacterController))]
public class MoveFeature : MonoBehaviour
{
    private const float MaxDeltaTime = 1f / 20f;

    [SerializeField] private float m_Gravity = -25f;
    [SerializeField, Tooltip("넉백이 줄어드는 빠르기")] private float m_ImpulseDamping = 9f;
    [SerializeField, Tooltip("초당 회전 각도")] private float m_TurnSpeed = 900f;

    private CharacterController m_Controller;
    private Vector3 m_Desired;
    private Vector3 m_Impulse;
    private float m_VerticalSpeed;

    public bool IsKnockedBack => m_Impulse.sqrMagnitude > 0.5f;
    public Vector3 Velocity => m_Desired + m_Impulse;

    // ==================================================

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (m_Controller.enabled == false)
            return;

        // 씬 로드 직후 같은 긴 프레임에 한 번에 크게 떨어지지 않게 자른다
        float dt = Mathf.Min(Time.deltaTime, MaxDeltaTime);
        if (dt <= 0f)
            return;

        if (m_Controller.isGrounded && m_VerticalSpeed < 0f)
            m_VerticalSpeed = -2f;
        m_VerticalSpeed += m_Gravity * dt;

        Vector3 motion = (m_Desired + m_Impulse) * dt;
        motion.y = m_VerticalSpeed * dt;
        m_Controller.Move(motion);

        m_Impulse = Vector3.Lerp(m_Impulse, Vector3.zero, 1f - Mathf.Exp(-m_ImpulseDamping * dt));
    }

    public void SetVelocity(Vector3 velocity)
    {
        velocity.y = 0f;
        m_Desired = velocity;
    }

    public void Stop()
    {
        m_Desired = Vector3.zero;
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
        transform.rotation = instant
            ? target
            : Quaternion.RotateTowards(transform.rotation, target, m_TurnSpeed * Time.deltaTime);
    }

    public void SetCollision(bool isOn)
    {
        m_Controller.enabled = isOn;
    }
}
