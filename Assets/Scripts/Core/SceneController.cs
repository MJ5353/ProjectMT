using UnityEngine;

[DefaultExecutionOrder(-1100)]
public sealed class SceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        GameManager.InitializeSingleton();
    }
}
