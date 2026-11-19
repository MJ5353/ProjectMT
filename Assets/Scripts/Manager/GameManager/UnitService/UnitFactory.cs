using UnityEngine;

// ResourceManager를 감싸 유닛을 만들고 돌려보낸다.
// 꺼낸 뒤 ID를 넣어 처음 상태로 되돌리고(OnSpawn), 돌려보내기 전에 정리한다(OnDespawn). UnitService만 쓴다
public sealed class UnitFactory
{
    public T Create<T>(T prefab, Vector3 position, Quaternion rotation, int unitID) where T : UnitController
    {
        ResourceManager resourceM = ResourceManager.Instance;
        if (resourceM == null)
        {
            CLogger.Error("UnitFactory", "ResourceManager가 없습니다. 씬에 SceneController가 있는지 확인하세요.");
            return null;
        }

        T unit = resourceM.Spawn(prefab, position, rotation);
        if (unit == null)
            return null;

        unit.OnSpawn(unitID);
        return unit;
    }

    public void Release(UnitController unit)
    {
        if (unit == null)
            return;

        unit.OnDespawn();

        ResourceManager resourceM = ResourceManager.Instance;
        if (resourceM != null)
            resourceM.Despawn(unit);
        else
            Object.Destroy(unit.gameObject);
    }
}
