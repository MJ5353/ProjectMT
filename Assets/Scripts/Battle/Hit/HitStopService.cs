using UnityEngine;

// 전투 중 전체 시간을 잠깐 멈춘다. 겹친 요청은 더 긴 쪽을 따른다.
public sealed class HitStopService : ServiceBase
{
    private float m_Remain;

    // ------------------------------------------------

    public void Request(float duration)
    {
        if (duration <= 0f)
            return;

        m_Remain = Mathf.Max(m_Remain, duration);
        TimeManager.Instance?.SetHitStop(true);
    }

    public override void Tick(float deltaTime)
    {
        if (m_Remain <= 0f)
            return;

        m_Remain -= Time.unscaledDeltaTime;
        if (m_Remain <= 0f)
            TimeManager.Instance?.SetHitStop(false);
    }

    // virtual ---

    public override void Clear()
    {
        m_Remain = 0f;
        TimeManager.Instance?.SetHitStop(false);
    }
}
