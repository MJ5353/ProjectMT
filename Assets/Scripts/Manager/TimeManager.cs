using UnityEngine;

// ==================================================

// 게임 시간을 멈추는 이유. 여러 이유가 겹칠 수 있어 플래그로 모으고, 하나라도 남아 있으면 멈춘다
[System.Flags]
public enum EPauseReason
{
    None = 0,
    App = 1 << 0,       // 앱이 백그라운드로 내려감(모바일)
    Focus = 1 << 1,     // 창 포커스를 잃음(PC)
    Confirm = 1 << 2,   // 확인 팝업이 묻는 중(토벌 종료 등)
    Market = 1 << 3,    // 현장 시장이 열려 있음
}

// ==================================================

// Time.timeScale을 한 곳에서만 바꾼다. 일시정지 이유가 하나라도 있거나 히트스톱 중이면 멈춘다.
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneController가 Create)
// !!) Time.timeScale을 직접 바꾸지 말고 여기를 거칠 것(히트스톱이 끝나며 일시정지를 풀어 버리는 일 방지)
// !!) 이유는 건 쪽이 직접 푼다. 다른 이유가 남아 있으면 풀어도 계속 멈춰 있다

// [mj] todo) local time <-> server time 보정

public class TimeManager : MonoSingleton<TimeManager>
{
    private EPauseReason m_PauseReason;
    private bool m_IsHitStop;

    public bool IsPaused => m_PauseReason != EPauseReason.None;
    public EPauseReason PauseReason => m_PauseReason;

    // ------------------------------------------------

    public void SetPause(EPauseReason reason, bool isPaused)
    {
        if (isPaused)
            m_PauseReason |= reason;
        else
            m_PauseReason &= ~reason;

        Apply();
    }

    public void SetHitStop(bool isHitStop)
    {
        m_IsHitStop = isHitStop;
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = IsPaused || m_IsHitStop ? 0f : 1f;
    }

    // virtual ---

    protected override void OnInit()
    {
        m_PauseReason = EPauseReason.None;
        m_IsHitStop = false;
        Apply();
    }

    protected override void OnClear()
    {
        m_PauseReason = EPauseReason.None;
        m_IsHitStop = false;
        Apply();
    }
}
