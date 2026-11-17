using UnityEngine;

// ResourceManager를 감싸 액터를 만들고 돌려보낸다.
// 꺼낸 뒤 ID를 넣어 처음 상태로 되돌리고(OnSpawn), 돌려보내기 전에 정리한다(OnDespawn). ActorService만 쓴다
public sealed class ActorFactory
{
    public T Create<T>(T prefab, Vector3 position, Quaternion rotation, int actorID) where T : PoolableMono, IActor
    {
        ResourceManager resourceM = ResourceManager.Instance;
        if (resourceM == null)
        {
            CLogger.Error("ActorFactory", "ResourceManager가 없습니다. 씬에 SceneController가 있는지 확인하세요.");
            return null;
        }

        T actor = resourceM.Spawn(prefab, position, rotation);
        if (actor == null)
            return null;

        actor.OnSpawn(actorID);
        return actor;
    }

    public void Release(IActor actor)
    {
        if (actor is not PoolableMono poolable || poolable == null)
            return;

        actor.OnDespawn();

        ResourceManager resourceM = ResourceManager.Instance;
        if (resourceM != null)
            resourceM.Despawn(poolable);
        else
            Object.Destroy(poolable.gameObject);
    }
}
