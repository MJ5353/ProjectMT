// ==================================================

// 땅을 파는 몬스터(버더지) 판단. MonsterBrain과 같고, 맞은 뒤 도망 대신 땅속에 숨었다가 대상 근처에서 나온다(AIHideState).
// 다른 판단을 새로 쓰지 않고 상태 하나만 더해 끼우는 예

public class BurrowBrain : MonsterBrain
{
    public BurrowBrain(MonsterController controller) : base(controller)
    {
        SetState(EAIState.Hide, new AIHideState(this));
    }

    protected override EAIState GetStateAfterHit()
    {
        return EAIState.Hide;
    }
}
