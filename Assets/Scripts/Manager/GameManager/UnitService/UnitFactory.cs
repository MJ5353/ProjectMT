using UnityEngine;

// ResourceManager를 감싸 유닛을 만들고 돌려보낸다.
// Def.PrefabPath(폴더 + ID)로 찾은 프리팹(몸)을 풀에서 꺼내 ID·Def를 넣어 처음 상태로 되돌리고(OnSpawn), 돌려보내기 전에 정리한다(OnDespawn). UnitService만 쓴다
public sealed class UnitFactory
{
    public UnitController Create(UnitDef def, Vector3 position, Quaternion rotation, int unitID)
    {
        if (def == null)
        {
            CLogger.Error("UnitFactory", "Def가 비어 있습니다.");
            return null;
        }

        ResourceManager resourceM = ResourceManager.Instance;
        if (resourceM == null)
        {
            CLogger.Error("UnitFactory", "ResourceManager가 없습니다. 씬에 SceneController가 있는지 확인하세요.");
            return null;
        }

        // 없으면 Load가 경로마다 한 번 오류 로그
        UnitController prefab = resourceM.Load<UnitController>(def.PrefabPath, $"{def.name}: 프리팹 이름이 Def ID와 같은지, 프리팹 빌드를 했는지 확인하세요.");
        if (prefab == null)
            return null;

        UnitController unit = resourceM.Spawn(prefab, position, rotation);
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

        ResourceManager.S_Despawn(unit); // 풀로 돌려준다(ResourceManager가 만든 것이 아니면 지운다)
    }
}
