using UnityEngine;

// ==================================================

// 땅속에 숨기(Burrow 피격 뒤 코어). 파고들기(Burrow) → 땅속에서 대상 쪽으로 이동(Underground, Def.HideTime) → 튀어나오기(Emerge) → 추적.
// 숨어 있는 동안(파고들기~땅속)은 맞지 않고 조준에서도 빠진다(HitF.SetHidden). 애니 상태 이름은 버더지 애니메이터(AC_002_Mole) 기준

public class BurrowCore : AICore
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

    public override ECoreState Type => ECoreState.Burrow;
    public override ECoreKind Kind => ECoreKind.Action;
    public override ECoreKind InterruptedBy => ECoreKind.Hit | ECoreKind.Dead; // 숨는 중에는 공격·대시가 끼지 않게

    // ------------------------------------------------

    public const EAISub Needs = EAISub.TargetSense;   // 이 코어가 쓰는 서브. 조립(MonsterDef.SetAI)이 프리셋 코어들의 Needs를 모아 서브를 붙인다(AICore.TryGetNeeds)

    public BurrowCore(UnitController owner, AIDef def) : base(owner, def) { }

    protected override void Enter()
    {
        base.Enter();
        Move(Vector3.zero, 0f);
        Owner.HitF.SetHidden(true);
        ChangePhase(EPhase.Burrow, Owner.AnimF.GetLength(BurrowClip), BurrowClip);
    }

    protected override void Decide(float deltaTime)
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
                    Owner.MoveF.Stop();
                    Owner.HitF.SetHidden(false);
                    ChangePhase(EPhase.Emerge, Owner.AnimF.GetLength(EmergeClip), EmergeClip);
                    break;
                }

                if (Target.HasTarget)
                {
                    Vector3 direction = Target.GetDirection();
                    Owner.MoveF.SetVelocity(direction * MoveSpeed);
                    Owner.MoveF.FaceTo(direction, deltaTime);
                }

                break;
            }
            case EPhase.Emerge:
            {
                Owner.MoveF.FaceTo(Target.GetDirection(), deltaTime);
                if (m_PhaseTime >= m_PhaseLength)
                    ChangeCore(ECoreState.Chase);

                break;
            }
        }
    }

    public override void Exit()
    {
        Owner.MoveF.Stop();
        Owner.HitF.SetHidden(false);
    }

    // ==================================================

    private void ChangePhase(EPhase phase, float length, string clip)
    {
        m_Phase = phase;
        m_PhaseTime = 0f;
        m_PhaseLength = length;
        Owner.AnimF.Play(clip, true);
    }
}
