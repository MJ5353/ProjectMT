using UnityEngine;

// 히트스톱 반응. 타격에 실린 시간만큼 HitStopManager에 멈춤을 요청한다.
public class HitStopFeature : MonoBehaviour, IHitReaction
{
    [SerializeField, Tooltip("타격 값에 곱하는 배율")] private float m_Scale = 1f;

    public int Order => 30;

    public void OnHit(in HitParams hit)
    {
        HitStopManager.Request(hit.HitStop * m_Scale);
    }
}

// 전체 시간을 잠깐 멈추는 관리자. 겹친 요청은 더 긴 쪽을 따른다. 처음 요청할 때 자동 생성된다.
public class HitStopManager : MonoBehaviour
{
    private static HitStopManager s_Instance;

    private float m_Remain;

    // ==================================================

    public static void Request(float duration)
    {
        if (duration <= 0f)
            return;

        if (s_Instance == null)
            s_Instance = new GameObject("[HitStopManager]").AddComponent<HitStopManager>();

        s_Instance.m_Remain = Mathf.Max(s_Instance.m_Remain, duration);
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (m_Remain <= 0f)
            return;

        m_Remain -= Time.unscaledDeltaTime;
        if (m_Remain <= 0f)
            Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (s_Instance == this)
            s_Instance = null;
    }
}
