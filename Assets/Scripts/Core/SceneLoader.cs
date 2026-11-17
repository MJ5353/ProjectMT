using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Lobby = "Lobby";
    public const string Battle = "Battle";
}

public static class SceneLoader
{
    public static IEnumerator Load(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
            yield break;

        while (op.isDone == false)
            yield return null;
    }
}
