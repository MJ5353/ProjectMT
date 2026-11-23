using System.Collections.Generic;
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

// 전체 슬로우를 거는 이유. 이유마다 배율을 따로 들고, 여러 개면 가장 느린(작은) 배율을 쓴다
public enum ESlowReason
{
    Rush,   // 퍼펙트 닷지 러시
}

// ==================================================

// Time.timeScale을 한 곳에서만 바꾼다. 일시정지 이유가 하나라도 있거나 히트스톱 중이면 멈추고, 아니면 슬로우 배율(없으면 1)을 쓴다.
// 슬로우 중에도 제 속도로 움직일 유닛(UnitDef.IsSlowImmune, 플레이어)은 PlayerTimeRate를 시간에 곱해 되돌린다
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneController가 Create)
// !!) Time.timeScale을 직접 바꾸지 말고 여기를 거칠 것(히트스톱이 끝나며 일시정지를 풀어 버리는 일 방지)
// !!) 이유는 건 쪽이 직접 푼다. 다른 이유가 남아 있으면 풀어도 계속 멈춰 있다

// [mj] todo) local time <-> server time 보정

public class TimeManager : MonoSingleton<TimeManager>
{
    private const float MinSlowRate = 0.01f;

    private EPauseReason m_PauseReason;
    private bool m_IsHitStop;
    private readonly Dictionary<ESlowReason, float> m_DictSlowRate = new(); // 건 이유별 배율
    private float m_SlowRate = 1f; // 건 이유 중 가장 작은 배율. 1이면 슬로우 없음

    public bool IsPaused => m_PauseReason != EPauseReason.None;
    public EPauseReason PauseReason => m_PauseReason;
    public bool IsSlow => m_SlowRate < 1f;

    // 슬로우를 무시하는 유닛이 시간에 곱할 배율(1/슬로우). 멈춤·히트스톱 때는 timeScale이 0이라 곱해도 0이 되므로 따로 0을 주지 않는다(0 나누기 없음)
    public float PlayerTimeRate => 1f / m_SlowRate;

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

    // 전체 슬로우를 이유째 건다. 0 이하는 멈춤과 섞이므로 받지 않는다
    public void SetSlow(ESlowReason reason, float rate)
    {
        m_DictSlowRate[reason] = Mathf.Clamp(rate, MinSlowRate, 1f);
        RefreshSlowRate();
    }

    public void ClearSlow(ESlowReason reason)
    {
        m_DictSlowRate.Remove(reason);
        RefreshSlowRate();
    }

    private void RefreshSlowRate()
    {
        m_SlowRate = 1f;
        foreach (float rate in m_DictSlowRate.Values)
        {
            m_SlowRate = Mathf.Min(m_SlowRate, rate);
        }

        Apply();
    }

    private void Apply()
    {
        Time.timeScale = IsPaused || m_IsHitStop ? 0f : m_SlowRate;
    }

    // virtual ---

    protected override void OnInit()
    {
        m_PauseReason = EPauseReason.None;
        m_IsHitStop = false;
        m_DictSlowRate.Clear();
        m_SlowRate = 1f;
        Apply();
    }

    protected override void OnClear()
    {
        m_PauseReason = EPauseReason.None;
        m_IsHitStop = false;
        m_DictSlowRate.Clear();
        m_SlowRate = 1f;
        Apply();
    }
}
