using UnityEngine;

// ==================================================

// 주인 따라가기(Pet 기본 코어). FollowDistance보다 멀면 주인 쪽으로 걷고(두 배보다 멀면 달리기), 가까우면 멈춘다.
// 주인은 SetLeader로 정하고, 없거나 죽었으면 플레이어

public class FollowCore : AICore
{
    private UnitController m_Leader;

    public override ECoreState Type => ECoreState.Follow;

    // ------------------------------------------------

    public const EAISub Needs = EAISub.None;   // 이 코어가 쓰는 서브. 조립(MonsterDef.SetAI)이 프리셋 코어들의 Needs를 모아 서브를 붙인다(AICore.TryGetNeeds)

    public FollowCore(UnitController owner, AIDef def) : base(owner, def) { }

    public void SetLeader(UnitController leader)
    {
        m_Leader = leader;
    }

    protected override void Decide(float deltaTime)
    {
        UnitController leader = GetLeader();
        if (leader == null)
        {
            Move(Vector3.zero, 0f);
            return;
        }

        Vector3 diff = leader.transform.position - Owner.transform.position;
        diff.y = 0f;

        float distance = diff.magnitude;
        if (distance <= Def.FollowDistance)
        {
            Move(Vector3.zero, 0f);
            return;
        }

        Move(diff, MoveSpeed, distance > Def.FollowDistance * 2f);
    }

    // get ---

    // 따라갈 주인. 정한 주인이 없거나 죽었으면 플레이어
    public UnitController GetLeader()
    {
        if (m_Leader != null && m_Leader.IsDead == false)
            return m_Leader;

        return FlowManager.Instance.Battle?.UnitS.Player;
    }
}
