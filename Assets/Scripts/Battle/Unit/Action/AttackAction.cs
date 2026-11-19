using UnityEngine;

// ==================================================

// 공격 하나. 예고(AttackSpec.WindupTime)가 있으면 먼저 움츠리며 대상 쪽으로 돌고, 그 뒤 MeleeAttackFeature로 클립·돌진·판정을 돌린다.
// 클립이 끝나면 끝. 판정 구간이 지나면 대시로, 거기서 캔슬 지연까지 지나면 다음 공격으로 끊을 수 있다(피격·사망은 언제나)

public class AttackAction : UnitAction
{
    private static readonly Vector3 s_SquashScale = new(1.15f, 0.8f, 1.15f); // 예고 끝 움츠림 배율

    private readonly float m_BlendTime;    // 공격 애니로 넘어갈 때 섞는 시간(UnitDef.AttackBlendTime)

    public override EActionType Type => EActionType.Attack;
    public override bool IsFinished => m_IsStarted && Owner.Melee.IsFinished(AttackTime);

    public AttackSpec Spec { get; private set; }
    public float CancelDelay { get; set; }  // 판정 뒤 이 시간이 지나면 다음 공격으로 끊을 수 있다. 0이면 판정 끝나자마자
    public bool CanBufferNext => m_IsStarted && AttackTime >= Spec.HitTime; // 판정 시작 뒤에 누른 것만 다음 타로 받는다(너무 이른 연타는 무시)

    private float AttackTime => Time - Spec.WindupTime; // 예고가 끝나고 클립이 시작된 뒤 지난 시간

    private Vector3 m_Direction;
    private Transform m_FaceTarget;
    private bool m_IsStarted;      // 예고가 끝나고 클립을 틀었는지
    private Vector3 m_BaseScale;   // 움츠리기 전 모델 크기

    // ------------------------------------------------

    public AttackAction(UnitController owner, float blendTime) : base(owner)
    {
        m_BlendTime = blendTime;
    }

    // direction이 0이면 지금 보는 방향 그대로. faceTarget이 있으면 예고 동안 그쪽으로 돈다(TurnSpeed)
    public void Setup(AttackSpec spec, Vector3 direction, Transform faceTarget)
    {
        Spec = spec;
        m_Direction = direction;
        m_FaceTarget = faceTarget;
    }

    public override bool CanInterruptBy(EActionType next)
    {
        switch (next)
        {
            case EActionType.Attack:
                return m_IsStarted && Owner.Melee.IsDone(AttackTime) && AttackTime >= Spec.HitEndTime + CancelDelay;
            case EActionType.Dash:
                return m_IsStarted && Owner.Melee.IsDone(AttackTime);
            default:
                return base.CanInterruptBy(next);
        }
    }

    protected override void Enter()
    {
        m_IsStarted = false;
        if (m_Direction.sqrMagnitude > 0.0001f)
            Owner.Move.FaceTo(m_Direction, true);

        if (Spec.WindupTime <= 0f)
        {
            StartClip();
            return;
        }

        // 예고: 멈춰서 움츠린다. 방향은 TurnSpeed로만 맞추고, 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        Owner.Move.Stop();
        Owner.Anim.Play("Idle");
        m_BaseScale = VisualRoot.localScale;
    }

    protected override void Update(float deltaTime)
    {
        if (m_IsStarted)
        {
            Owner.Melee.UpdateAttack(AttackTime);
            return;
        }

        if (m_FaceTarget != null)
            Owner.Move.FaceTo(m_FaceTarget.position - Owner.transform.position);

        float t = Mathf.Clamp01(Time / Spec.WindupTime);
        VisualRoot.localScale = Vector3.Scale(m_BaseScale, Vector3.Lerp(Vector3.one, s_SquashScale, t));
        if (t >= 1f)
        {
            VisualRoot.localScale = m_BaseScale;
            StartClip();
        }
    }

    public override void Exit()
    {
        if (m_IsStarted == false && Spec.WindupTime > 0f)
            VisualRoot.localScale = m_BaseScale;

        Owner.Move.Stop();
        Owner.Melee.End();
    }

    // ==================================================

    private Transform VisualRoot => Owner.Property.Animator.transform;

    private void StartClip()
    {
        m_IsStarted = true;
        Owner.Melee.StartAttack(Spec, m_BlendTime);
    }
}
