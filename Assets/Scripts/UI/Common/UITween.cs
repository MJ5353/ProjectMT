using System;
using System.Collections;
using UnityEngine;

// ==================================================

// UI 연출용 감속 곡선과 공용 연출 루프. t는 0~1로 자른다

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

    // duration 동안 매 프레임 onUpdate(0~1)를 부르고, 끝에 1로 한 번 더 부른다.
    // !!) 시장·전투 종료 중에는 timeScale이 멈춰 있을 수 있어 unscaled 시간으로 돈다
    public static IEnumerator CoPlay(float duration, Action<float> onUpdate)
    {
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            onUpdate(Mathf.Clamp01(time / duration));
            yield return null;
        }

        onUpdate(1f);
    }
}
