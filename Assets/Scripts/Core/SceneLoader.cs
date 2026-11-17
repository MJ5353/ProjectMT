using System.Collections;
using UnityEngine.SceneManagement;

namespace MushTaker.Core
{
    public static class SceneNames
    {
        public const string Lobby = "Lobby";
        public const string Battle = "Battle";
    }

    public static class SceneLoader
    {
        public static IEnumerator Load(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
        }
    }
}
