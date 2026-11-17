using UnityEngine;

// Time.timeScale을 한 곳에서만 바꾼다. 일시정지와 히트스톱 중 하나라도 켜져 있으면 멈춘다.
// !!) Time.timeScale을 직접 바꾸지 말고 여기를 거칠 것(히트스톱이 끝나며 일시정지를 풀어 버리는 일 방지)
public static class GameTime
{
    private static bool s_IsPaused;
    private static bool s_IsHitStop;

    public static bool IsPaused => s_IsPaused;

    // ==================================================

    public static void SetPause(bool isPaused)
    {
        s_IsPaused = isPaused;
        Apply();
    }

    internal static void SetHitStop(bool isHitStop)
    {
        s_IsHitStop = isHitStop;
        Apply();
    }

    // 새 플레이·씬 정리 때 처음 상태로
    internal static void Reset()
    {
        s_IsPaused = false;
        s_IsHitStop = false;
        Apply();
    }

    private static void Apply()
    {
        Time.timeScale = s_IsPaused || s_IsHitStop ? 0f : 1f;
    }
}
