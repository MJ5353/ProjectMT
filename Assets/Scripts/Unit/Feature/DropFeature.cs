using UnityEngine;

// ==================================================

// 사망 보상. 체력이 0이 되는 순간 버섯을 뿌리고(MonsterDef.ListDrop) 처치를 알린다(MonsterKilled). 판단과 상관없이 몬스터가 붙인다

public class DropFeature : UnitFeature
{
    private readonly MonsterDef m_Def;

    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    // ------------------------------------------------

    public DropFeature(UnitController owner, MonsterDef def) : base(owner)
    {
        m_Def = def;
        owner.HealthF.Died += OnDied;
    }

    private void OnDied()
    {
        SpawnDrops();

        UnitService units = GameManager.Instance?.UnitS;
        if (units != null)
            SEventManager.Publish(new MonsterKilled(units, Owner.UnitID, m_Def, Owner.transform.position));
    }

    private void SpawnDrops()
    {
        Vector3 origin = Owner.transform.position + Vector3.up * Define.MonsterDropHeight;
        foreach (MonsterDef.DropEntry drop in m_Def.ListDrop)
        {
            if (drop.Mushroom == null || Random.value > drop.Chance)
                continue;

            MushroomPickup prefab = ResourceManager.Instance.Load<MushroomPickup>(drop.Mushroom.PickupPath, "데이터 편집기 > 버섯 > 픽업 빌드");
            if (prefab == null)
                continue;

            int count = Random.Range(drop.Min, drop.Max + 1);
            for (int i = 0; i < count; i++)
            {
                MushroomPickup pickup = ResourceManager.Instance.Spawn(prefab, origin, Quaternion.identity);
                Vector2 spread = Random.insideUnitCircle * Define.MonsterDropSpread;
                pickup.Launch(new Vector3(spread.x, Define.MonsterDropUpSpeed, spread.y), EFreshness.Low, BattleService.RollRarity());
            }
        }
    }
}
