
// ==================================================

// 동료 판단(가벼운 예). 주인을 따라다니기만 한다(AIFollowState). 싸우기·도망은 없다.
// 주인을 정하지 않으면 플레이어를 따른다. 동료 유닛을 만들 때 CreateBrain에서 쓴다

public class PetBrain : AIBrain
{
    private UnitController m_Leader;

    public float FollowDistance { get; }   // 이보다 멀어지면 따라간다
    public float MoveSpeed { get; }
    protected override EAIState StartState => EAIState.Follow;

    // ------------------------------------------------

    public PetBrain(UnitController owner, float followDistance, float moveSpeed) : base(owner)
    {
        FollowDistance = followDistance;
        MoveSpeed = moveSpeed;
        
        SetState(EAIState.Follow, new AIFollowState(this));
    }

    public void SetLeader(UnitController leader)
    {
        m_Leader = leader;
    }

    // 따라갈 주인. 정한 주인이 없거나 죽었으면 플레이어
    public UnitController GetLeader()
    {
        if (m_Leader != null && m_Leader.IsDead == false)
            return m_Leader;

        return GameManager.Instance?.ActorS?.Player;
    }
}
