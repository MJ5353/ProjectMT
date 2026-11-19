using UnityEngine;

// ==================================================

// 몬스터 판단. AI 공통 동작(대기·배회·추적·도망·사망 뒤, 공격 고르기·콤보)은 AIBrain이 하고,
// 여기는 몬스터만의 일(사망 때 버섯 드롭·처치 알림)만 한다. 수치는 MonsterDef.
// 행동이 다른 몬스터는 이것을 이어받아 상태만 바꿔 끼운다(BurrowBrain). 어느 판단을 쓸지는 MonsterDef.Brain

public class MonsterBrain : AIBrain
{
    private readonly MonsterController m_Controller;

    // ------------------------------------------------

    public MonsterBrain(MonsterController controller) : base(controller, controller.Def)
    {
        m_Controller = controller;
    }

    public override void OnDeadStart()
    {
        SpawnDrops();

        ActorService actors = GameManager.Instance?.ActorS;
        if (actors != null)
            SEventManager.Publish(new MonsterKilled(actors, m_Controller.ActorID, m_Controller.Def, m_Controller.transform.position));
    }

    // ==================================================

    private void SpawnDrops()
    {
        Vector3 origin = m_Controller.transform.position + Vector3.up * 0.6f;
        foreach (MonsterDef.DropEntry drop in m_Controller.Def.ListDrop)
        {
            if (drop.Pickup == null || Random.value > drop.Chance)
                continue;

            int count = Random.Range(drop.Min, drop.Max + 1);
            for (int i = 0; i < count; i++)
            {
                MushroomPickup pickup = ResourceManager.Instance.Spawn(drop.Pickup, origin, Quaternion.identity);
                Vector2 spread = Random.insideUnitCircle * 2f;
                pickup.Launch(new Vector3(spread.x, 5f, spread.y), EFreshness.Low);
            }
        }
    }
}
