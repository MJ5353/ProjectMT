using UnityEngine;

// 3인칭 쿼터뷰 추적 카메라. 시작할 때 플레이어를 따라간다
public class FollowCamera : MonoBehaviour
{
    private const float Distance = 11f;     // 바라보는 점에서 카메라까지 거리
    private const float Pitch = 50f;        // 내려다보는 각도
    private const float Yaw = 0f;
    private const float LookHeight = 1f;    // 바라보는 점 높이(발밑 기준)
    private const float SmoothTime = 0.12f;

    private Transform m_Target;
    private Vector3 m_Velocity;

    // ==================================================

    private void Start()
    {
        if (PlayerController.Current != null)
            SetTarget(PlayerController.Current.transform);
    }

    private void LateUpdate()
    {
        if (m_Target == null)
            return;

        transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(), ref m_Velocity, SmoothTime);
        transform.rotation = GetRotation();
    }

    public void SetTarget(Transform target)
    {
        m_Target = target;
        if (m_Target == null)
            return;

        transform.SetPositionAndRotation(GetDesiredPosition(), GetRotation());
    }

    private static Quaternion GetRotation()
    {
        return Quaternion.Euler(Pitch, Yaw, 0f);
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 lookPoint = m_Target.position + Vector3.up * LookHeight;
        return lookPoint - GetRotation() * Vector3.forward * Distance;
    }
}
