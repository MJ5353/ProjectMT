using UnityEngine;

// 몹 FSM 상태들. 수치는 전부 MobDef에서 읽는다
// 흐름: 대기 ↔ 배회 → (발견) 추적 → 움츠림 → 공격 → 추적 / 맞으면 피격 → (도망) → 추적 / 사망

public abstract class MobState : IState
{
    protected readonly MobController m_Mob;

    protected MobDef Def => m_Mob.Def;
    protected float TimeInState => m_Mob.TimeInState;

    protected MobState(MobController mob)
    {
        m_Mob = mob;
    }

    // ==================================================

    // virtual---

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }
}

public class MobIdleState : MobState
{
    private float m_Duration;

    public MobIdleState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        m_Mob.Move.Stop();
        m_Mob.Anim.Play("Idle");
        m_Duration = Random.Range(1f, 2.5f);
    }

    public override void Update(float deltaTime)
    {
        if (m_Mob.CanSeeTarget())
        {
            m_Mob.ChangeState(EMobState.Chase);
            return;
        }

        if (TimeInState >= m_Duration)
            m_Mob.ChangeState(EMobState.Wander);
    }
}

public class MobWanderState : MobState
{
    private const float MaxDuration = 3f;
    private Vector3 m_Destination;

    public MobWanderState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        Vector2 offset = Random.insideUnitCircle * Def.WanderRadius;
        m_Destination = m_Mob.HomePosition + new Vector3(offset.x, 0f, offset.y);
        m_Mob.Anim.Play("Walk");
    }

    public override void Update(float deltaTime)
    {
        if (m_Mob.CanSeeTarget())
        {
            m_Mob.ChangeState(EMobState.Chase);
            return;
        }

        Vector3 diff = m_Destination - m_Mob.transform.position;
        diff.y = 0f;
        if (diff.magnitude < 0.3f || TimeInState >= MaxDuration)
        {
            m_Mob.ChangeState(EMobState.Idle);
            return;
        }

        m_Mob.MoveTo(diff, Def.WanderSpeed);
    }
}

public class MobChaseState : MobState
{
    public MobChaseState(MobController mob) : base(mob) { }

    public override void Update(float deltaTime)
    {
        float distance = m_Mob.GetTargetDistance();
        bool isTargetGone = PlayerController.Current == null || PlayerController.Current.IsDead;
        if (isTargetGone || distance > Def.LoseRange)
        {
            m_Mob.ChangeState(EMobState.Idle);
            return;
        }

        // 사거리 안: 쿨이 돌면 공격, 아니면 마주 보고 대기
        if (distance <= Def.AttackRange)
        {
            m_Mob.Move.Stop();
            m_Mob.Move.FaceTo(m_Mob.GetTargetDirection());
            m_Mob.Anim.Play("Idle");

            if (m_Mob.CooldownRemain <= 0f)
                m_Mob.ChangeState(EMobState.Windup);

            return;
        }

        m_Mob.Anim.Play("Walk");
        m_Mob.MoveTo(m_Mob.GetTargetDirection(), Def.MoveSpeed);
    }
}

// 공격 예고. 몸을 납작하게 움츠려서 플레이어가 보고 빠질 수 있게 한다
public class MobWindupState : MobState
{
    private static readonly Vector3 SquashScale = new(1.15f, 0.8f, 1.15f);
    private Vector3 m_BaseScale;

    public MobWindupState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        m_Mob.Move.Stop();
        m_Mob.Anim.Play("Idle");
        m_BaseScale = m_Mob.VisualRoot.localScale;
    }

    public override void Update(float deltaTime)
    {
        m_Mob.Move.FaceTo(m_Mob.GetTargetDirection());

        float t = Def.WindupTime > 0f ? Mathf.Clamp01(TimeInState / Def.WindupTime) : 1f;
        m_Mob.VisualRoot.localScale = Vector3.Scale(m_BaseScale, Vector3.Lerp(Vector3.one, SquashScale, t));

        if (t >= 1f)
            m_Mob.ChangeState(EMobState.Attack);
    }

    public override void Exit()
    {
        m_Mob.VisualRoot.localScale = m_BaseScale;
    }
}

public class MobAttackState : MobState
{
    private bool m_IsHitDone;
    private float m_Length;

    public MobAttackState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        AttackSpec attack = Def.Attack;
        m_IsHitDone = false;
        m_Length = m_Mob.Anim.GetLength(attack.Clip);

        m_Mob.Move.Stop();
        m_Mob.Move.FaceTo(m_Mob.GetTargetDirection(), true);
        m_Mob.Anim.Play(attack.Clip, true);

        if (attack.Lunge > 0f)
            m_Mob.Move.AddImpulse(m_Mob.transform.forward * attack.Lunge);
    }

    public override void Update(float deltaTime)
    {
        AttackSpec attack = Def.Attack;
        if (m_IsHitDone == false && TimeInState >= attack.HitTime)
        {
            m_IsHitDone = true;
            m_Mob.Melee.Perform(attack);
        }

        if (TimeInState >= m_Length)
            m_Mob.ChangeState(EMobState.Chase);
    }

    public override void Exit()
    {
        m_Mob.CooldownRemain = Def.AttackCooldown;
    }
}

public class MobHitState : MobState
{
    public MobHitState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        m_Mob.Move.Stop();
        m_Mob.Anim.Play("Hit", true);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState < Def.HitStunTime)
            return;

        m_Mob.ChangeState(Def.FleeTime > 0f ? EMobState.Flee : EMobState.Chase);
    }
}

// 버끼처럼 겁 많은 몹: 맞으면 반대로 달아났다가 다시 덤빈다
public class MobFleeState : MobState
{
    public MobFleeState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        m_Mob.Anim.Play("Walk");
        m_Mob.Anim.SetSpeed(1.6f);
    }

    public override void Update(float deltaTime)
    {
        if (TimeInState >= Def.FleeTime)
        {
            m_Mob.ChangeState(EMobState.Chase);
            return;
        }

        Vector3 away = -m_Mob.GetTargetDirection();
        m_Mob.MoveTo(away, Def.FleeSpeed);
    }

    public override void Exit()
    {
        m_Mob.Anim.SetSpeed(1f);
    }
}

public class MobDeadState : MobState
{
    private const float CollisionOffTime = 0.4f;
    private const float DestroyTime = 2f;

    private bool m_IsCollisionOff;

    public MobDeadState(MobController mob) : base(mob) { }

    public override void Enter()
    {
        m_IsCollisionOff = false;
        m_Mob.Move.Stop();
        m_Mob.Anim.SetSpeed(1f);
        m_Mob.Anim.Play("Death", true);

        SpawnDrops();
        EventBus.Publish(new MobKilled(Def, m_Mob.transform.position));
    }

    public override void Update(float deltaTime)
    {
        // 넉백으로 밀려난 뒤 충돌을 끈다
        if (m_IsCollisionOff == false && TimeInState >= CollisionOffTime)
        {
            m_IsCollisionOff = true;
            m_Mob.Move.SetCollision(false);
        }

        if (TimeInState >= DestroyTime)
            Object.Destroy(m_Mob.gameObject);
    }

    private void SpawnDrops()
    {
        Vector3 origin = m_Mob.transform.position + Vector3.up * 0.6f;
        foreach (MobDef.DropEntry drop in Def.ListDrop)
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
