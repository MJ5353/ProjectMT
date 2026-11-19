using UnityEngine;

// ==================================================

public class MonsterDeadState : MonsterState
{
    private const float CollisionOffTime = 0.4f;
    private const float DestroyTime = 2f;

    private bool m_IsCollisionOff;

    // ------------------------------------------------

    public MonsterDeadState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_IsCollisionOff = false;
        m_Controller.Anim.SetSpeed(1f); // 멈춤·Death 애니는 사망 행동(DeadAction)이 한다

        SpawnDrops();
        ActorService actors = GameManager.Instance?.ActorS;
        
        if (actors != null)
            SEventManager.Publish(new MonsterKilled(actors, m_Controller.ActorID, Def, m_Controller.transform.position));
    }

    public override void Update(float deltaTime)
    {
        // 넉백으로 밀려난 뒤 충돌을 끈다
        if (m_IsCollisionOff == false && TimeInState >= CollisionOffTime)
        {
            m_IsCollisionOff = true;
            m_Controller.Move.SetCollision(false);
        }

        if (TimeInState >= DestroyTime)
            m_Controller.Despawn();
    }

    private void SpawnDrops()
    {
        Vector3 origin = m_Controller.transform.position + Vector3.up * 0.6f;
        foreach (MonsterDef.DropEntry drop in Def.ListDrop)
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
