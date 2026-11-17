using UnityEngine;

// ==================================================

// ResourceManager가 만들고 돌려받아 다시 쓰는 MonoBehaviour의 기반.
// 다시 쓰이므로 꺼낼 때(OnPoolSpawn) 상태를 처음으로 되돌린다

public abstract class PoolableMono : MonoBehaviour
{
    public int PoolKey { get; private set; }   // 원본 프리팹 InstanceID. 0이면 ResourceManager 밖에서 만든 것

    // ==================================================

    public void SetPoolKey(int poolKey)
    {
        PoolKey = poolKey;
    }

    // virtual---

    public virtual void OnPoolSpawn() { }
    public virtual void OnPoolDespawn() { }
}
