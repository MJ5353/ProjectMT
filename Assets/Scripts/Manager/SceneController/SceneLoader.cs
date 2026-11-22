using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================

public static class SceneLoader
{
    // 다음 씬이 다 읽혀도 minSeconds(unscaled)가 지나기 전에는 넘어가지 않는다(가짜 로딩. 그동안 OverlayLoading이 덮고 있다).
    // onProgress(0~1)는 실제 읽은 정도와 흐른 시간 비율 중 느린 쪽이라, 빨리 읽혀도 minSeconds 동안 차오른다
    public static IEnumerator Load(string sceneName, float minSeconds, Action<float> onProgress)
    {
        float startTime = Time.unscaledTime;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
            yield break;

        // !!) allowSceneActivation이 false면 progress는 0.9에서 멈춘다(isDone이 되지 않는다)
        op.allowSceneActivation = false;
        while (true)
        {
            float timeRatio = minSeconds > 0f ? (Time.unscaledTime - startTime) / minSeconds : 1f;
            float progress = Mathf.Min(op.progress / 0.9f, timeRatio);
            onProgress?.Invoke(progress);

            if (progress >= 1f)
                break;

            yield return null;
        }

        op.allowSceneActivation = true;
        while (op.isDone == false)
            yield return null;
    }

    // 읽을 씬 없이 minSeconds(unscaled) 동안 진행바만 채운다(다시 하기처럼 씬을 바꾸지 않을 때의 가짜 로딩)
    public static IEnumerator Wait(float minSeconds, Action<float> onProgress)
    {
        float startTime = Time.unscaledTime;
        float ratio = 0f;

        while (ratio < 1f)
        {
            ratio = minSeconds > 0f ? Mathf.Clamp01((Time.unscaledTime - startTime) / minSeconds) : 1f;
            onProgress?.Invoke(ratio);
            yield return null;
        }
    }
}
