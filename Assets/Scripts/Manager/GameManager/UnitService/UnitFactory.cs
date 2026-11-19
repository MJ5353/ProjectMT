using UnityEngine;

// ResourceManager를 감싸 유닛을 만들고 돌려보낸다.
// Def의 프리팹(몸)을 풀에서 꺼내 ID·Def를 넣어 처음 상태로 되돌리고(OnSpawn), 돌려보내기 전에 정리한다(OnDespawn). UnitService만 쓴다
public sealed class UnitFactory
{
    public UnitController Create(UnitDef def, Vector3 position, Quaternion rotation, int unitID)
    {
        if (def == null || def.Prefab == null)
        {
            CLogger.Error("UnitFactory", $"Def 또는 Def.Prefab이 비어 있습니다: {(def == null ? "null" : def.name)}");
            return null;
        }

        ResourceManager resourceM = ResourceManager.Instance;
        if (resourceM == null)
        {
            CLogger.Error("UnitFactory", "ResourceManager가 없습니다. 씬에 SceneController가 있는지 확인하세요.");
            return null;
        }

        UnitController unit = resourceM.Spawn(def.Prefab, position, rotation);
        if (unit == null)
            return null;

        unit.OnSpawn(unitID, def);

        // 조립 실패(오류 로그는 OnSpawn에서)면 등록하지 않고 돌려보낸다
        if (unit.enabled == false)
        {
            Release(unit);
            return null;
        }

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
