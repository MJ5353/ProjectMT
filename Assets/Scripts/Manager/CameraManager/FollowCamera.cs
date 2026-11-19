using UnityEngine;

// ==================================================

// 3인칭 쿼터뷰 추적 카메라. 시작할 때 플레이어를 따라간다
// [mj] todo) 더 멋진 전투 연출 카메라

public class FollowCamera : MonoBehaviour
{
    private const float Distance = 11f;     // 바라보는 점에서 카메라까지 거리
    private const float Pitch = 50f;        // 내려다보는 각도
    private const float Yaw = 0f;
    private const float LookHeight = 1f;    // 바라보는 점 높이(발밑 기준)
    private const float SmoothTime = 0.12f;

    private Transform m_Target;
    private Vector3 m_Velocity;
    private Vector3 m_BasePosition; // 흔들림을 뺀 추적 위치. 흔들림이 SmoothDamp에 섞이지 않게 따로 든다

    // ------------------------------------------------

    private void Start()
    {
        UnitController player = GameManager.Instance?.UnitS?.Player;
        if (player != null)
            SetTarget(player.transform);
    }

    private void LateUpdate()
    {
        if (m_Target == null)
            return;

        m_BasePosition = Vector3.SmoothDamp(m_BasePosition, GetDesiredPosition(), ref m_Velocity, SmoothTime);
        
        float shake = CameraManager.Instance?.ShakeAmount ?? 0f;
        Quaternion rotation = GetRotation();
        transform.SetPositionAndRotation(m_BasePosition + rotation * Vector3.forward * (Distance * shake), rotation);
    }

    public void SetTarget(Transform target)
    {
        m_Target = target;
        if (m_Target == null)
            return;

        m_BasePosition = GetDesiredPosition();
        transform.SetPositionAndRotation(m_BasePosition, GetRotation());
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
