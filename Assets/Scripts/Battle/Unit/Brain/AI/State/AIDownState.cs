using UnityEngine;

// ==================================================

// 쓰러짐(BossBrain: 부위가 깨졌을 때). PartBreak 애니 뒤 Down 애니로 Def.DownTime 동안 누워 있다가 추적으로 간다.
// 그동안 공격하지 않아 플레이어가 마음껏 칠 수 있는 틈이 된다

public class AIDownState : MonsterState
{
    private const string PartBreakClip = "PartBreak";
    private const string DownClip = "Down";

    private float m_BreakLength;
    private bool m_IsDown;

    // ------------------------------------------------

    public AIDownState(MonsterBrain ai) : base(ai) { }

    public override void Enter()
    {
        m_Controller.ActionLayer.SetMove(Vector3.zero, 0f);
        m_BreakLength = m_Controller.AnimF.GetLength(PartBreakClip);
        m_IsDown = false;
        m_Controller.AnimF.Play(PartBreakClip, true);
    }

    public override void Update(float deltaTime)
    {
        if (m_IsDown == false && TimeInState >= m_BreakLength)
        {
            m_IsDown = true;
            m_Controller.AnimF.Play(DownClip, true);
        }

        // 누운 채 맞으면 피격 애니가 덮으므로 다시 튼다(같은 애니가 재생 중이면 무시)
        m_Controller.AnimF.Play(m_IsDown ? DownClip : PartBreakClip);

        if (TimeInState >= m_BreakLength + Def.DownTime)
            m_AI.Change(EAIState.Chase);
    }
}
