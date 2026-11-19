using UnityEngine;

// ==================================================

// 몬스터 판단. 대기·배회·추적·도망·사망 뒤 상태와 공격 고르기(AttackChoice)·콤보를 돌린다. 수치는 MonsterDef.
// 행동이 다른 몬스터는 이것을 이어받아 상태를 더하거나 바꿔 끼운다(BurrowBrain·BossBrain). 어느 판단을 쓸지는 MonsterDef.Brain

public class MonsterBrain : AIBrain
{
    private readonly MonsterController m_Controller;

    public MonsterDef Def => m_Controller.Def;
    public AttackChoice AttackChoice { get; }                   // 공격 고르기·쿨

    // ------------------------------------------------

    public MonsterBrain(MonsterController controller) : base(controller)
    {
        m_Controller = controller;
        AttackChoice = new AttackChoice(controller.Def, controller.Def.AttackCooldown);

        SetState(EAIState.Idle, new AIIdleState(this));
        SetState(EAIState.Wander, new AIWanderState(this));
        SetState(EAIState.Chase, new AIChaseState(this));
        SetState(EAIState.Flee, new AIFleeState(this));
        SetState(EAIState.Dead, new AIDeadState(this));
    }

    public override void Reset()
    {
        AttackChoice.Reset();
        base.Reset();
    }

    protected override void Sense(float deltaTime)
    {
        AttackChoice.Tick(deltaTime);
        Target.Refresh(Def.SightRange);
    }

    // 공격은 끝날 때마다 쿨을 걸고, 끝까지 했으면 콤보 다음 타. 피격 경직이 풀리면 GetStateAfterHit
    protected override void OnActionEnded(EActionType action, bool isFinished)
    {
        if (Owner.IsDead)
            return;

        switch (action)
        {
            case EActionType.Attack:
            {
                AttackChoice.StartCooldown();
                AttackSpec comboNext = Owner.ActionLayer.ComboNext;

                if (isFinished && comboNext != null)
                    Owner.ActionLayer.RequestAttack(comboNext, Vector3.zero, Target.TargetTransform);

                break;
            }
            case EActionType.Hit:
            {
                if (isFinished)
                    Change(GetStateAfterHit());

                break;
            }
        }
    }

    // 사망 상태에 들어갈 때(AIDeadState): 버섯 드롭·처치 알림
    public void OnDeadStart()
    {
        SpawnDrops();

        ActorService actors = GameManager.Instance?.ActorS;
        if (actors != null)
            SEventManager.Publish(new MonsterKilled(actors, m_Controller.ActorID, Def, m_Controller.transform.position));
    }

    // ==================================================

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

    // virtual ---

    // 피격 경직이 풀린 뒤 갈 상태. 기본은 도망(FleeTime이 있으면) 또는 바로 추적
    protected virtual EAIState GetStateAfterHit()
    {
        return Def.FleeTime > 0f ? EAIState.Flee : EAIState.Chase;
    }
}
