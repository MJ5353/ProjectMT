using UnityEngine;

// ==================================================

// 전투 중 전체 시간을 잠깐 멈춘다. 겹친 요청은 더 긴 쪽을 따른다.
// 슬로우(퍼펙트 닷지 러시) 중에는 받지 않는다. 연타마다 화면이 멈춰 템포가 끊기므로 번쩍·파편만 남긴다

public sealed class HitStopService : ServiceBase
{
    private float m_Remain;

    // ------------------------------------------------

    public void Request(float duration)
    {
        if (duration <= 0f || TimeManager.Instance.IsSlow)
            return;

        m_Remain = Mathf.Max(m_Remain, duration);
        TimeManager.Instance.SetHitStop(true);
    }

    public override void Tick(float deltaTime)
    {
        if (m_Remain <= 0f)
            return;

        m_Remain -= Time.unscaledDeltaTime;
        if (m_Remain <= 0f)
            TimeManager.Instance.SetHitStop(false);
    }

    // virtual ---

    public override void Clear()
    {
        m_Remain = 0f;
        TimeManager.Instance.SetHitStop(false);
    }
}
