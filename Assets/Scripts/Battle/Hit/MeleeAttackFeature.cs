using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 근접 공격 판정. Begin으로 공격을 걸고, 매 프레임 TickAttack(공격 시작부터 지난 시간)을 부르면
// 판정 구간(HitTime~HitEndTime) 동안 공격 구체와 후보 액터의 몸을 비교한다. 한 공격에서 맞은 대상은 다시 맞지 않는다.

public class MeleeAttackFeature : UnitFeature
{
    private readonly List<ActorHitCandidate> m_ListCandidate = new();
    private readonly HashSet<UnitController> m_SetHit = new(); // 이번 공격에 이미 맞은 대상

    // [mj] todo) remove?
    public AttackSpec Spec => m_Spec;
    private AttackSpec m_Spec;
    
    // 판정을 한 번이라도 했는지(한 프레임 판정 보장)
    private bool m_IsTicked; 
    private int m_HitRound; // 다시 맞히기 회차(AttackSpec.RehitFrame). 바뀌면 맞은 대상 목록을 비운다
    private float m_Length;  // 지금 공격 클립 길이(초)

    // ------------------------------------------------

    public MeleeAttackFeature(UnitController owner, IReadOnlyList<AttackSpec> listAttack) : base(owner)
    {
        // 판정 부위 뼈가 프리팹에 없으면 몸 앞으로 판정한다
        foreach (AttackSpec attack in listAttack)
        {
            if (attack.HitPart != EBattlePartType.None && owner.Property.GetBattleBone(attack.HitPart) == null)
                CLogger.Warning("MeleeAttackFeature", $"판정 부위 뼈가 프리팹에 없음: {attack.HitPart} ({owner.transform.name})", owner.transform);
        }
    }

    public override void Reset()
    {
        End();
    }

    public override void Clear()
    {
        End();
    }

    // 판정 구 중심. 판정 뼈가 있으면 뼈 위치(Height·Reach 무시), 없으면 몸 앞
    public static Vector3 GetCenter(Transform owner, AttackSpec spec, Transform bone)
    {
        if (bone != null)
            return bone.position;

        return owner.position + Vector3.up * spec.Height + owner.forward * spec.Reach;
    }

    // 공격 실행 ---

    // 공격 하나 시작: 판정 걸기, 멈춰서 클립 재생, 시작 돌진(Lunge). 방향은 부르는 쪽이 먼저 맞춘다
    public void StartAttack(AttackSpec spec, float blendTime = -1f)
    {
        Begin(spec);
        m_Length = Owner.AnimF.GetLength(spec.Clip);

        Owner.MoveF.Stop();
        Owner.AnimF.Play(spec.Clip, true, blendTime);

        if (spec.Lunge > 0f)
            Owner.MoveF.AddImpulse(Owner.transform.forward * spec.Lunge);
    }

    // 공격 중 매 프레임(time = 공격 시작부터 지난 시간): 판정 구간 동안 앞으로 돌진하고 판정한다
    public void UpdateAttack(float time)
    {
        if (m_Spec == null)
            return;

        if (m_Spec.ActiveMoveSpeed > 0f)
            Owner.MoveF.SetVelocity(IsActive(time) ? Owner.transform.forward * m_Spec.ActiveMoveSpeed : Vector3.zero);

        TickAttack(time);
    }

    // 클립이 끝났는지(걸린 공격이 없어도 true)
    public bool IsFinished(float time)
    {
        return m_Spec == null || time >= m_Length;
    }

    // 판정 ---

    public void Begin(AttackSpec spec)
    {
        m_Spec = spec;
        m_IsTicked = false;
        m_HitRound = 0;
        m_SetHit.Clear();
    }

    public void End()
    {
        m_Spec = null;
        m_IsTicked = false;
        m_SetHit.Clear();
    }

    // 판정 구간 안인지. 끝 프레임이 시작 이하면 첫 판정 한 프레임만
    public bool IsActive(float time)
    {
        if (m_Spec == null || time < m_Spec.HitTime)
            return false;

        return m_IsTicked == false || time <= m_Spec.HitEndTime;
    }

    // 판정 구간이 지났는지(걸린 공격이 없어도 true)
    public bool IsDone(float time)
    {
        return m_Spec == null || (m_IsTicked && time > m_Spec.HitEndTime);
    }

    // 공격 시작부터 지난 시간. 판정 구간이면 판정하고 새로 맞힌 수를 돌려준다
    public int TickAttack(float time)
    {
        if (IsActive(time) == false)
            return 0;

        if (m_IsTicked == false)
            PlayEffect(m_Spec);

        m_IsTicked = true;
        if (m_Spec.RehitTime > 0f)
        {
            int round = Mathf.FloorToInt((time - m_Spec.HitTime) / m_Spec.RehitTime);
            if (round != m_HitRound)
            {
                m_HitRound = round;
                m_SetHit.Clear();
            }
        }

        return Perform(m_Spec);
    }

    // 판정 자리(땅 이펙트는 발밑)에 주인 방향으로 터뜨리고 판정 반지름만큼 키운다
    private void PlayEffect(AttackSpec spec)
    {
        EffectService effectS = GameManager.Instance?.EffectS;
        if (spec.Effect == null || effectS == null)
            return;

        Transform owner = Owner.transform;
        Vector3 position = GetCenter(owner, spec, Owner.Property.GetBattleBone(spec.HitPart));
        if (spec.Effect.IsOnGround)
            position.y = owner.position.y;

        effectS.Play(spec.Effect, position, owner.rotation, spec.Radius);
    }

    private int Perform(AttackSpec spec)
    {
        Transform owner = Owner.transform;
        ActorService actors = GameManager.Instance?.ActorS;

        if (actors == null)
            return 0;

        Transform bone = Owner.Property.GetBattleBone(spec.HitPart);

        Vector3 center = GetCenter(owner, spec, bone);
        ETeam team = Owner.HitF.Team;
        actors.CollectNearbyEnemies(center, spec.Radius, team, m_ListCandidate);

        int hitCount = 0;
        foreach (ActorHitCandidate candidate in m_ListCandidate)
        {
            if (m_SetHit.Contains(candidate.Actor))
                continue;

            CharacterController controller = candidate.Actor.HurtBody;
            if (controller == null || controller.enabled == false || controller.gameObject.activeInHierarchy == false)
                continue;

            if (candidate.OverlapsSphere(center, spec.Radius, out Vector3 point, out int partIndex) == false)
                continue;

            Vector3 direction = candidate.Actor.transform.position - owner.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : owner.forward;

            // 무적 등으로 막히면 구간 안에서 다시 노린다
            HitParams hit = new(owner.gameObject, team, spec.Damage, direction, spec.Knockback, spec.HitStop, point, partIndex);
            if (candidate.Actor.HitF.Receive(hit) == false)
                continue;

            m_SetHit.Add(candidate.Actor);
            hitCount++;
        }

        m_ListCandidate.Clear();
        return hitCount;
    }
}
