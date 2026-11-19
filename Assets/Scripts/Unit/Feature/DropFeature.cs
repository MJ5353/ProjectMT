using UnityEngine;

// ==================================================

// 사망 보상. 체력이 0이 되는 순간 버섯을 뿌리고(MonsterDef.ListDrop) 처치를 알린다(MonsterKilled). 판단과 상관없이 몬스터가 붙인다

public class DropFeature : UnitFeature
{
    private readonly MonsterDef m_Def;

    private const float DropHeight = 0.6f;      // 버섯이 튀어나오는 높이
    private const float SpreadRadius = 2f;      // 옆으로 퍼지는 반경
    private const float LaunchUpSpeed = 5f;     // 위로 튀는 속도

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
        Vector3 origin = Owner.transform.position + Vector3.up * DropHeight;
        foreach (MonsterDef.DropEntry drop in m_Def.ListDrop)
        {
            if (drop.Pickup == null || Random.value > drop.Chance)
                continue;

            int count = Random.Range(drop.Min, drop.Max + 1);
            for (int i = 0; i < count; i++)
            {
                MushroomPickup pickup = ResourceManager.Instance.Spawn(drop.Pickup, origin, Quaternion.identity);
                Vector2 spread = Random.insideUnitCircle * SpreadRadius;
                pickup.Launch(new Vector3(spread.x, LaunchUpSpeed, spread.y), EFreshness.Low);
            }
        }
    }
}
