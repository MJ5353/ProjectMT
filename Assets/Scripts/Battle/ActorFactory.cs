using UnityEngine;

// 액터를 띄우는 공장. 지금은 Instantiate·Destroy만 한다
// 풀링은 다음 단계에서 여기에 붙인다
public sealed class ActorFactory
{
    public T Create<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        return Object.Instantiate(prefab, position, rotation);
    }

    public void Release(Component actor)
    {
        if (actor == null)
            return;

        Object.Destroy(actor.gameObject);
    }
}
