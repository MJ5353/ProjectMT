using UnityEngine;

// ==================================================

// 땅속에 숨기(BurrowBrain). 파고들기(Burrow) → 땅속에서 대상 쪽으로 이동(Underground, Def.HideTime) → 튀어나오기(Emerge) → 추적.
// 숨어 있는 동안(파고들기~땅속)은 무적. 애니 상태 이름은 버더지 애니메이터(AC_002_Mole) 기준

public class AIHideState : AIState
{
    private const string BurrowClip = "Burrow";
    private const string UndergroundClip = "Underground";
    private const string EmergeClip = "Emerge";
    private const float EmergeDistance = 1.2f;  // 대상과 이만큼 가까우면 일찍 나온다

    private enum EPhase
    {
        Burrow,
        Underground,
        Emerge,
    }

    private EPhase m_Phase;
    private float m_PhaseTime;
    private float m_PhaseLength;

    // ------------------------------------------------

    public AIHideState(AIBrain ai) : base(ai) { }

    public override void Enter()
    {
        m_Controller.ActionLayer.SetMove(Vector3.zero, 0f);
        float burrowLength = m_Controller.AnimF.GetLength(BurrowClip);
        m_Controller.HitF.SetInvincible(burrowLength + Def.HideTime);
        ChangePhase(EPhase.Burrow, burrowLength, BurrowClip);
    }

    public override void Update(float deltaTime)
    {
        m_PhaseTime += deltaTime;
        switch (m_Phase)
        {
            case EPhase.Burrow:
            {
                if (m_PhaseTime >= m_PhaseLength)
                    ChangePhase(EPhase.Underground, Def.HideTime, UndergroundClip);

                break;
            }
            case EPhase.Underground:
            {
                // 대상 쪽으로 파고든다. 가까워지거나 시간이 다 되면 나온다
                bool isNear = Target.HasTarget && Target.GetDistance() <= EmergeDistance;
                if (isNear || m_PhaseTime >= m_PhaseLength)
                {
                    m_Controller.MoveF.Stop();
                    ChangePhase(EPhase.Emerge, m_Controller.AnimF.GetLength(EmergeClip), EmergeClip);
                    break;
                }

                if (Target.HasTarget)
                    m_Controller.MoveF.SetVelocity(Target.GetDirection() * Def.MoveSpeed);

                break;
            }
            case EPhase.Emerge:
            {
                m_Controller.MoveF.FaceTo(Target.GetDirection());
                if (m_PhaseTime >= m_PhaseLength)
                    m_AI.Change(EAIState.Chase);

                break;
            }
        }
    }

    public override void Exit()
    {
        m_Controller.MoveF.Stop();
    }

    // ==================================================

    private void ChangePhase(EPhase phase, float length, string clip)
    {
        m_Phase = phase;
        m_PhaseTime = 0f;
        m_PhaseLength = length;
        m_Controller.AnimF.Play(clip, true);
    }
}
