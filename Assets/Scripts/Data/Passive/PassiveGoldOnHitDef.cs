using UnityEngine;

// ==================================================

// 맞으면 골드 동전을 땅에 떨군다(상해보험). 동전은 버섯처럼 주워야 들어오고, 주우면 바로 인벤토리로 간다(BattleService)
[CreateAssetMenu(menuName = "MushTaker/Data/Passive/GoldOnHit", fileName = "Passive_")]
public class PassiveGoldOnHitDef : PassiveDef
{
    [field: SerializeField]
    public Vector2Int GoldRange { get; private set; } = new(5, 10);    // 떨구는 골드(최소~최대). 동전 하나 = 1골드

    // ------------------------------------------------

    public override void OnHit(UnitController owner, in HitParams hit)
    {
        MushroomPickup prefab = ResourceManager.Instance.Load<MushroomPickup>(CommonPath.GoldPickup, "데이터 편집기 > 버섯 > 픽업 빌드");
        if (prefab == null)
            return;

        int count = Random.Range(GoldRange.x, GoldRange.y + 1);
        MushroomPickup.S_Drop(prefab, owner.transform.position, count, EFreshness.High, ERarity.Common);
    }

#if UNITY_EDITOR
    public void EditorSetup(Vector2Int goldRange)
    {
        GoldRange = goldRange;
    }
#endif
}
