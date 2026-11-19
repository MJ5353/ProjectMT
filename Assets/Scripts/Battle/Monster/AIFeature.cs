
// ==================================================

// 판단 상태. 행동(공격·피격 등)은 행동 층(EActionType)이 맡고, 여기는 "무엇을 할지"만
public enum EAIState
{
    Idle,
    Wander,
    Chase,
    Flee,
    Dead,
}

// ==================================================

// 몬스터 판단 층. AI 상태(대기·배회·추적·도망·사망 뒤)를 돌리며 행동 층(ActionFeature)에 요청만 한다.
// 공격·피격·대시 행동 중에는 판단을 쉬고, 행동이 끝나면(ActionFeature.Ended) 주인이 다음 상태를 정한다.
// 주인이 전투 중일 때만 UpdateAI를 부른다(전투가 끝나면 Stop)

public class AIFeature : UnitFeature
{
    private readonly StateMachine<EAIState> m_Fsm = new();

    public EAIState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;

    // ------------------------------------------------

    public AIFeature(UnitController owner) : base(owner) { }

    public void Add(EAIState key, IState state)
    {
        m_Fsm.Add(key, state);
    }

    public override void Reset()
    {
        m_Fsm.Change(EAIState.Idle);
    }

    public void UpdateAI(float deltaTime)
    {
        if (IsActing())
            return;

        m_Fsm.Update(deltaTime);
    }

    public void Change(EAIState next)
    {
        m_Fsm.Change(next);
    }

    // 진행 중인 상태를 Exit로 정리(애니 배속 복구)하고 판단을 끈다
    public void Stop()
    {
        m_Fsm.Stop();
    }

    // ==================================================

    // 공격·피격·대시는 행동 층이 끝까지 맡는다. 사망은 사망 뒤 처리(드롭·사라짐)를 위해 판단을 계속 돈다
    private bool IsActing()
    {
        EActionType action = Owner.Action.Current;
        return action == EActionType.Attack || action == EActionType.Hit || action == EActionType.Dash;
    }
}
