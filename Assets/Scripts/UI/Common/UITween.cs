using UnityEngine;

// ==================================================

// UI 연출용 감속 곡선. t는 0~1로 자른다

public static class UITween
{
    private const float BackC1 = 1.70158f;
    private const float BackC3 = BackC1 + 1f;

    // ------------------------------------------------

    public static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
    }

    // 살짝 넘쳤다가 돌아온다
    public static float EaseOutBack(float t)
    {
        float x = Mathf.Clamp01(t) - 1f;
        return 1f + BackC3 * x * x * x + BackC1 * x * x;
    }
}
