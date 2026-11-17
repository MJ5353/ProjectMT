using UnityEngine;

// Time.timeScale을 한 곳에서만 바꾼다. 일시정지와 히트스톱 중 하나라도 켜져 있으면 멈춘다.
// GameManager 오브젝트에 붙어 씬이 바뀌어도 남는다(SceneController가 만든다)
// !!) Time.timeScale을 직접 바꾸지 말고 여기를 거칠 것(히트스톱이 끝나며 일시정지를 풀어 버리는 일 방지)
public class TimeManager : MonoSingleton<TimeManager>
{
    private bool m_IsPaused;
    private bool m_IsHitStop;

    public bool IsPaused => m_IsPaused;

    // ==================================================

    public void SetPause(bool isPaused)
    {
        m_IsPaused = isPaused;
        Apply();
    }

    internal void SetHitStop(bool isHitStop)
    {
        m_IsHitStop = isHitStop;
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = m_IsPaused || m_IsHitStop ? 0f : 1f;
    }

    // virtual---

    protected override void OnInit()
    {
        m_IsPaused = false;
        m_IsHitStop = false;
        Apply();
    }

    protected override void OnClear()
    {
        m_IsPaused = false;
        m_IsHitStop = false;
        Apply();
    }
}
