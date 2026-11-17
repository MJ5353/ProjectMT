using UnityEngine;

// 전투 중 전체 시간을 잠깐 멈춘다. 겹친 요청은 더 긴 쪽을 따른다.
public class HitStopManager : MonoSingleton<HitStopManager>
{
    private float m_Remain;

    // ==================================================

    public void Request(float duration)
    {
        if (duration <= 0f)
            return;

        m_Remain = Mathf.Max(m_Remain, duration);
        GameTime.SetHitStop(true);
    }

    internal void Tick(float unscaledDeltaTime)
    {
        if (m_Remain <= 0f)
            return;

        m_Remain -= unscaledDeltaTime;
        if (m_Remain <= 0f)
            GameTime.SetHitStop(false);
    }

    // virtual---

    protected override void OnClear()
    {
        m_Remain = 0f;
        GameTime.SetHitStop(false);
    }
}
