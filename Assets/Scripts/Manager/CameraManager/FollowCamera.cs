using UnityEngine;

// ==================================================

// 3인칭 쿼터뷰 추적 카메라. 시작할 때 플레이어를 따라간다
// [mj] todo) 더 멋진 전투 연출 카메라

public class FollowCamera : MonoBehaviour
{
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    private Transform m_Target;
    private Vector3 m_Velocity;
    private Vector3 m_BasePosition; // 흔들림을 뺀 추적 위치. 흔들림이 SmoothDamp에 섞이지 않게 따로 든다

    // ------------------------------------------------

    private void Start()
    {
        UnitController player = FlowManager.Instance.Battle?.UnitS.Player;
        if (player != null)
            SetTarget(player.transform);
    }

    private void LateUpdate()
    {
        if (m_Target == null)
            return;

        m_BasePosition = Vector3.SmoothDamp(m_BasePosition, GetDesiredPosition(), ref m_Velocity, Define.CameraSmoothTime);
        
        float shake = CameraManager.Instance.ShakeAmount;
        Quaternion rotation = GetRotation();
        transform.SetPositionAndRotation(m_BasePosition + rotation * Vector3.forward * (Define.CameraDistance * shake), rotation);
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
        return Quaternion.Euler(Define.CameraPitch, Define.CameraYaw, 0f);
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 lookPoint = m_Target.position + Vector3.up * Define.CameraLookHeight;
        return lookPoint - GetRotation() * Vector3.forward * Define.CameraDistance;
    }
}
