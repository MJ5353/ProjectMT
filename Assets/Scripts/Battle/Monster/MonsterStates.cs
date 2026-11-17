using UnityEngine;

// 몬스터 FSM 상태들. 수치는 전부 MonsterDef에서 읽는다
// 흐름: 대기 ↔ 배회 → (발견) 추적 → 움츠림 → 공격 → 추적 / 맞으면 피격 → (도망) → 추적 / 사망

public abstract class MonsterState : IState
{
    protected readonly MonsterController m_Controller;

    protected MonsterDef Def => m_Controller.Def;
    protected float TimeInState => m_Controller.TimeInState;

    protected MonsterState(MonsterController controller)
    {
        m_Controller = controller;
    }

    // ==================================================

    // virtual---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}

public class MonsterIdleState : MonsterState
{
    private float m_Duration;

    public MonsterIdleState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Move.Stop();
        m_Controller.Anim.Play("Idle");
        m_Duration = Random.Range(1f, 2.5f);
    }

    public override void Update(float deltaTime)
    {
        if (m_Controller.CanSeeTarget())
        {
            m_Controller.ChangeState(EMonsterState.Chase);
            return;
        }

        if (TimeInState >= m_Duration)
            m_Controller.ChangeState(EMonsterState.Wander);
    }
}

public class MonsterWanderState : MonsterState
{
    private const float MaxDuration = 3f;
    private Vector3 m_Destination;

    public MonsterWanderState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        Vector2 offset = Random.insideUnitCircle * Def.WanderRadius;
        m_Destination = m_Controller.HomePosition + new Vector3(offset.x, 0f, offset.y);
        m_Controller.Anim.Play("Walk");
    }

    public override void Update(float deltaTime)
    {
        if (m_Controller.CanSeeTarget())
        {
            m_Controller.ChangeState(EMonsterState.Chase);
            return;
        }

        Vector3 diff = m_Destination - m_Controller.transform.position;
        diff.y = 0f;
        if (diff.magnitude < 0.3f || TimeInState >= MaxDuration)
        {
            m_Controller.ChangeState(EMonsterState.Idle);
            return;
        }

        m_Controller.MoveTo(diff, Def.WanderSpeed);
    }
}

public class MonsterChaseState : MonsterState
{
    public MonsterChaseState(MonsterController controller) : base(controller) { }

    public override void Update(float deltaTime)
    {
        float distance = m_Controller.GetTargetDistance();
        bool isTargetGone = m_Controller.GetValidPlayer() == null;
        if (isTargetGone || distance > Def.LoseRange)
        {
            m_Controller.ChangeState(EMonsterState.Idle);
            return;
        }

        // 사거리 안: 쿨이 돌면 공격, 아니면 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            m_Controller.Move.Stop();
            m_Controller.Move.FaceTo(m_Controller.GetTargetDirection());
            m_Controller.Anim.Play("Idle");

            if (m_Controller.CooldownRemain <= 0f)
                m_Controller.ChangeState(EMonsterState.Windup);

            return;
        }

        m_Controller.Anim.Play("Walk");
        m_Controller.MoveTo(m_Controller.GetTargetDirection(), Def.MoveSpeed);
    }
}

// 공격 예고. 몸을 납작하게 움츠려서 플레이어가 보고 빠질 수 있게 한다
public class MonsterWindupState : MonsterState
{
    private static readonly Vector3 SquashScale = new(1.15f, 0.8f, 1.15f);
    private Vector3 m_BaseScale;

    public MonsterWindupState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Move.Stop();
        m_Controller.Anim.Play("Idle");
        m_BaseScale = m_Controller.VisualRoot.localScale;
    }

    public override void Update(float deltaTime)
    {
        m_Controller.Move.FaceTo(m_Controller.GetTargetDirection());

        float t = Def.WindupTime > 0f ? Mathf.Clamp01(TimeInState / Def.WindupTime) : 1f;
        m_Controller.VisualRoot.localScale = Vector3.Scale(m_BaseScale, Vector3.Lerp(Vector3.one, SquashScale, t));

        if (t >= 1f)
            m_Controller.ChangeState(EMonsterState.Attack);
    }

    public override void Exit()
    {
        m_Controller.VisualRoot.localScale = m_BaseScale;
    }
}

public class MonsterAttackState : MonsterState
{
    private bool m_IsHitDone;
    private float m_Length;

    public MonsterAttackState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        AttackSpec attack = Def.Attack;
        m_IsHitDone = false;
        m_Length = m_Controller.Anim.GetLength(attack.Clip);

        m_Controller.Move.Stop();
        m_Controller.Move.FaceTo(m_Controller.GetTargetDirection(), true);
        m_Controller.Anim.Play(attack.Clip, true);

        if (attack.Lunge > 0f)
            m_Controller.Move.AddImpulse(m_Controller.transform.forward * attack.Lunge);
    }

    public override void Update(float deltaTime)
    {
        AttackSpec attack = Def.Attack;
        if (m_IsHitDone == false && TimeInState >= attack.HitTime)
        {
            m_IsHitDone = true;
            m_Controller.Melee.Perform(attack);
        }

        if (TimeInState >= m_Length)
            m_Controller.ChangeState(EMonsterState.Chase);
    }

    public override void Exit()
    {
        m_Controller.CooldownRemain = Def.AttackCooldown;
    }
}

public class MonsterHitState : MonsterState
{
    public MonsterHitState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Move.Stop();
        m_Controller.Anim.Play("Hit", true);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState < Def.HitStunTime)
            return;

        m_Controller.ChangeState(Def.FleeTime > 0f ? EMonsterState.Flee : EMonsterState.Chase);
    }
}

// 버끼처럼 겁 많은 몬스터: 맞으면 반대로 달아났다가 다시 덤빈다
public class MonsterFleeState : MonsterState
{
    public MonsterFleeState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_Controller.Anim.Play("Walk");
        m_Controller.Anim.SetSpeed(1.6f);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState >= Def.FleeTime)
        {
            m_Controller.ChangeState(EMonsterState.Chase);
            return;
        }

        Vector3 away = -m_Controller.GetTargetDirection();
        m_Controller.MoveTo(away, Def.FleeSpeed);
    }

    public override void Exit()
    {
        m_Controller.Anim.SetSpeed(1f);
    }
}

public class MonsterDeadState : MonsterState
{
    private const float CollisionOffTime = 0.4f;
    private const float DestroyTime = 2f;

    private bool m_IsCollisionOff;

    public MonsterDeadState(MonsterController controller) : base(controller) { }

    public override void Enter()
    {
        m_IsCollisionOff = false;
        m_Controller.Move.Stop();
        m_Controller.Anim.SetSpeed(1f);
        m_Controller.Anim.Play("Death", true);

        SpawnDrops();
        ActorManagement actors = GameManager.Instance?.ActiveActors;
        if (actors != null)
            SEventService.Publish(new MonsterKilled(actors, m_Controller.ActorID, Def, m_Controller.transform.position));
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
                MushroomPickup pickup = Object.Instantiate(drop.Pickup, origin, Quaternion.identity);
                Vector2 spread = Random.insideUnitCircle * 2f;
                pickup.Launch(new Vector3(spread.x, 5f, spread.y));
            }
        }
    }
}
