using UnityEngine;

// 3인칭 쿼터뷰 추적 카메라. 각도·거리는 인스펙터에서 정한다
public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform m_Target = null;
    [SerializeField] private float m_Distance = 11f;
    [SerializeField] private float m_Pitch = 50f;
    [SerializeField] private float m_Yaw = 0f;
    [SerializeField, Tooltip("바라보는 점 높이")] private float m_LookHeight = 1f;
    [SerializeField] private float m_SmoothTime = 0.12f;

    private Vector3 m_Velocity;

    // ==================================================

    private void Start()
    {
        Init();
    }

    private void LateUpdate()
    {
        if (m_Target == null)
            return;

        transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(), ref m_Velocity, m_SmoothTime);
        transform.rotation = GetRotation();
    }

    public void Init()
    {
        if (m_Target == null && PlayerController.Current != null)
            m_Target = PlayerController.Current.transform;

        if (m_Target == null)
            return;

        transform.SetPositionAndRotation(GetDesiredPosition(), GetRotation());
    }

    public void SetTarget(Transform target)
    {
        m_Target = target;
        Init();
    }

    private Quaternion GetRotation()
    {
        return Quaternion.Euler(m_Pitch, m_Yaw, 0f);
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 lookPoint = m_Target.position + Vector3.up * m_LookHeight;
        return lookPoint - GetRotation() * Vector3.forward * m_Distance;
    }
}
