using System.Diagnostics;
using Debug = UnityEngine.Debug;

// ==================================================

// 로그 유틸. 태그를 붙여 출력하고, Log는 개발 빌드에서만 남는다(릴리스에서는 호출 자체가 빠진다)
public static class CLogger
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string tag, string message, UnityEngine.Object context = null)
    {
        Debug.Log($"[{tag}] {message}", context);
    }

    public static void Warning(string tag, string message, UnityEngine.Object context = null)
    {
        Debug.LogWarning($"[{tag}] {message}", context);
    }

    public static void Error(string tag, string message, UnityEngine.Object context = null)
    {
        Debug.LogError($"[{tag}] {message}", context);
    }
}
