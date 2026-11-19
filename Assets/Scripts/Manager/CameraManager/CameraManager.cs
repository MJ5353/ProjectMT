using UnityEngine;

// ==================================================

// 지금 씬의 메인 카메라를 들고 있다. 씬 컨트롤러가 시작할 때 등록하고 사라질 때 지운다(Camera.main 검색 대신).
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneControllerBase가 Create). 흔들림·보스전 구도 같은 연출도 이후 여기에 모은다

public class CameraManager : MonoSingleton<CameraManager>
{
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    public Camera Main { get; private set; }
    private float m_ShakeStrength;
    private float m_ShakeTime;
    private float m_ShakeStart;
    
    public Transform MainTransform => Main != null ? Main.transform : null;

    // 지금 카메라가 다가가 있는 정도(카메라 거리 비율). 실제 시간 기준이라 히트스톱 중에도 움직인다
    public float ShakeAmount
    {
        get
        {
            if (m_ShakeTime <= 0f)
                return 0f;

            float t = (Time.unscaledTime - m_ShakeStart) / m_ShakeTime;
            if (t >= 1f)
                return 0f;

            float curve = t < Define.ShakeInRatio ? t / Define.ShakeInRatio : Mathf.SmoothStep(0f, 1f, (1f - t) / (1f - Define.ShakeInRatio));
            return m_ShakeStrength * curve;
        }
    }

    // ------------------------------------------------

    // 겹친 요청은 지금 다가가 있는 정도보다 셀 때만 새로 시작한다
    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f || strength < ShakeAmount)
            return;

        m_ShakeStrength = strength;
        m_ShakeTime = duration;
        m_ShakeStart = Time.unscaledTime;
    }

    public void SetMain(Camera mainCamera)
    {
        Main = mainCamera;
        if (Main == null)
            CLogger.Warning("CameraManager", "씬 컨트롤러에 메인 카메라가 연결되지 않았습니다.");
    }

    // 다음 씬이 먼저 등록했으면 건드리지 않는다
    public void ClearMain(Camera mainCamera)
    {
        if (Main == mainCamera)
            Main = null;
    }

    protected override void OnClear()
    {
        Main = null;
        m_ShakeTime = 0f;
    }
}
