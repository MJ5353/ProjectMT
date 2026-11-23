using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 공격 하나. 예고(AttackSpec.WindupTime)가 있으면 먼저 움츠리며 대상 쪽으로 돌고, 그 뒤 클립·돌진·판정을 돌린다.
// 클립이 끝나면 끝. 판정 구간이 지나면 대시로, 거기서 캔슬 지연까지 지나면 다음 공격으로 끊을 수 있다(피격·사망은 언제나)
// 여기는 흐름(예고 → 클립 → 끝)과 끊기 규칙만 맡고, 일은 부품에 넘긴다: 판정 AttackHitter, 바닥 예고 AttackTelegraph, 퍼펙트 닷지 알림 AttackDodgeNotifier.
// 예고·알림은 몬스터 공격(플레이어가 피할 공격)만. 범위는 매 프레임 AttackArea 하나로 계산해 세 부품이 같이 쓴다
// 시간: Time은 실제로 흐른 시간, AttackTime은 클립 시간(공격 속도가 곱해짐). 클립 시간을 실제 시간으로 바꿀 땐 ToRealTime

public class AttackCore : CoreState
{
    private static readonly Vector3 s_SquashScale = new(1.15f, 0.8f, 1.15f); // 예고 끝 움츠림 배율
    private readonly float m_BlendTime;    // 공격 애니로 넘어갈 때 섞는 시간(UnitDef.AttackBlendTime)
    private readonly AttackHitter m_Hitter;
    private readonly AttackTelegraph m_Telegraph = new();
    private readonly AttackDodgeNotifier m_DodgeNotifier;
    
    private Vector3 m_Direction;
    private Transform m_FaceTarget;
    private bool m_IsStarted;      // 예고가 끝나고 클립을 틀었는지
    private Vector3 m_BaseScale;   // 움츠리기 전 모델 크기
    private float m_Length;        // 지금 공격 클립 길이(초)
    
    public AttackSpec Spec { get; private set; }  // 지금(끝난 뒤엔 마지막) 공격. 콤보 다음 타를 여기서 읽는다
    public float CancelDelay { get; set; }  // 판정 뒤 이 시간이 지나면 다음 공격으로 끊을 수 있다. 0이면 판정 끝나자마자
    public bool IsQuickCancel { get; set; } // 켜져 있으면 CancelDelay를 무시하고 판정 끝나자마자 끊는다(StateFeature.SetQuickCancel)

    public override ECoreState Type => ECoreState.Attack;
    public override ECoreKind Kind => ECoreKind.Action;
    public override ECoreKind InterruptedBy => ECoreKind.Hit | ECoreKind.Dead; // 다음 공격·대시는 CanInterruptBy에서 따로 본다
    public override bool IsFinished => m_IsStarted && AttackTime >= m_Length;
    
    public bool CanBufferNext => m_IsStarted && AttackTime >= Spec.HitTime; // 판정 시작 뒤에 누른 것만 다음 타로 받는다(너무 이른 연타는 무시)
    public float AttackTime => (Time - Spec.WindupTime) * AttackRate; // 예고가 끝나고 클립이 시작된 뒤 지난 클립 시간
    private float AttackRate => Owner.StatF.GetRate(EStatType.AttackSpeed); // 공격 속도 배율. 클립·판정이 이만큼 빨라진다
    private float HitAt => Spec.WindupTime + ToRealTime(Spec.HitTime); // 시작부터 첫 판정까지 실제 시간
    private Transform VisualRoot => Owner.Property.Animator.transform;
    private bool IsCharge => Spec.ActiveMoveSpeed > 0f; // 판정 구간 동안 앞으로 나아가는 돌진
    private bool IsMonsterAttack => Owner.Team != ETeam.Player; // 플레이어가 피할 공격(바닥 예고·퍼펙트 닷지)
    private float TelegraphProgress => HitAt > 0f ? Time / HitAt : 1f; // 바닥 예고 채움. 1이면 판정
    private bool IsHitDone => m_IsStarted && m_Hitter.IsDone(Spec, AttackTime);

    // ------------------------------------------------

    public AttackCore(UnitController owner, UnitDef def) : base(owner)
    {
        m_BlendTime = def.AttackBlendTime;
        m_Hitter = new AttackHitter(owner);
        m_DodgeNotifier = new AttackDodgeNotifier(owner, this);
        m_Hitter.TargetHit += m_DodgeNotifier.OnTargetHit;

        // 판정 부위 뼈가 프리팹에 없으면 몸 앞으로 판정한다
        List<AttackSpec> listAttack = new();
        def.CollectAttacks(listAttack);
        
        foreach (AttackSpec attack in listAttack)
        {
            if (attack.HitPart != EBattlePartType.None && owner.Property.GetBattleBone(attack.HitPart) == null)
                CLogger.Warning("AttackCore", $"판정 부위 뼈가 프리팹에 없음: {attack.HitPart} ({owner.transform.name})", owner.transform);
        }
    }

    // 판정 구 중심. 판정 뼈가 있으면 뼈 위치(Height·Reach 무시), 없으면 몸 앞
    public static Vector3 GetCenter(Transform owner, AttackSpec spec, Transform bone)
    {
        if (bone != null)
            return bone.position;

        return owner.position + Vector3.up * spec.Height + owner.forward * spec.Reach;
    }

    // direction이 0이면 지금 보는 방향 그대로. faceTarget이 있으면 예고 동안 그쪽으로 돈다(TurnSpeed)
    public void Setup(AttackSpec spec, Vector3 direction, Transform faceTarget)
    {
        Spec = spec;
        m_Direction = direction;
        m_FaceTarget = faceTarget;
    }

    // 풀에서 다시 꺼낼 때 마지막 공격을 잊는다(콤보가 이어지지 않게)
    public void ClearSpec()
    {
        Spec = null;
        m_Hitter.Clear();
    }

    // ---
    
    public override bool CanInterruptBy(CoreState next)
    {
        switch (next.Type)
        {
            case ECoreState.Attack:
                return IsHitDone && AttackTime >= Spec.HitEndTime + (IsQuickCancel ? 0f : CancelDelay);
            case ECoreState.Dash:
                return IsHitDone;
            default:
                return base.CanInterruptBy(next);
        }
    }

    protected override void Enter()
    {
        m_IsStarted = false;
        m_DodgeNotifier.Begin();
        
        if (m_Direction.sqrMagnitude > 0.0001f)
            Owner.MoveF.FaceToInstant(m_Direction);

        if (IsMonsterAttack)
            m_Telegraph.Show(IsCharge);

        if (Spec.WindupTime <= 0f)
        {
            StartClip();
            m_Telegraph.Refresh(GetArea(), TelegraphProgress);
            
            return;
        }

        // 예고: 멈춰서 움츠린다. 방향은 TurnSpeed로만 맞추고, 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        Owner.MoveF.Stop();
        Owner.AnimF.Play(AnimKey.Idle);
        
        m_BaseScale = VisualRoot.localScale;
        m_Telegraph.Refresh(GetArea(), TelegraphProgress);
    }

    protected override void Update(float deltaTime)
    {
        AttackArea area = GetArea();
        m_Telegraph.Refresh(area, TelegraphProgress);
        TrackDodge(area);

        if (m_IsStarted)
        {
            UpdateClip(AttackTime, area);
            return;
        }

        if (m_FaceTarget != null)
            Owner.MoveF.FaceTo(m_FaceTarget.position - Owner.transform.position, deltaTime);

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

        Owner.MoveF.Stop();
        Owner.AnimF.SetSpeed(1f);
        
        m_Hitter.Clear();
        m_Telegraph.Hide();
        m_DodgeNotifier.End();
    }
    
    // ---

    // 클립 시간 → 실제로 흐르는 시간(공격 속도만큼 짧아진다)
    private float ToRealTime(float clipTime)
    {
        return clipTime / AttackRate;
    }

    // 이번 프레임 범위. 돌진은 판정 구간에서 아직 나아갈 거리만큼 길다(판정 전이면 전체 거리)
    private AttackArea GetArea()
    {
        Transform owner = Owner.transform;
        Vector3 center = GetCenter(owner, Spec, Owner.Property.GetBattleBone(Spec.HitPart));

        float length = 0f;
        if (IsCharge)
        {
            float clipTime = Mathf.Max(AttackTime, Spec.HitTime);
            length = Spec.ActiveMoveSpeed * ToRealTime(Mathf.Max(0f, Spec.HitEndTime - clipTime));
        }

        return new AttackArea(center, owner.position.y, owner.forward, DamageCalculator.GetRadius(Owner, Spec), length);
    }

    // 판정이 닿기 전까지 범위에 든 적에게 알린다. 일반 공격은 첫 판정 전까지, 돌진은 판정 구간 끝까지
    private void TrackDodge(in AttackArea area)
    {
        if (IsMonsterAttack == false)
            return;

        if (m_Hitter.IsTicked && (IsCharge == false || AttackTime > Spec.HitEndTime))
            return;

        m_DodgeNotifier.Track(area, Mathf.Max(0f, HitAt - Time), Spec.ActiveMoveSpeed);
    }

    // 클립 시작: 판정 걸기, 멈춰서 클립 재생, 시작 돌진(Lunge). 방향은 Enter에서 먼저 맞춘다
    private void StartClip()
    {
        m_IsStarted = true;
        m_Hitter.Clear();
        m_Length = Owner.AnimF.GetLength(Spec.Clip);

        Owner.MoveF.Stop();
        Owner.AnimF.Play(Spec.Clip, true, m_BlendTime);
        Owner.AnimF.SetSpeed(AttackRate);

        if (Spec.Lunge > 0f)
            Owner.MoveF.AddImpulse(Owner.transform.forward * Spec.Lunge);
    }

    // 클립 중 매 프레임(time = 클립 시간): 판정 구간 동안 앞으로 돌진하고 판정한 뒤, 안 맞은 적에게 알린다
    // 일반 공격은 첫 판정 때 모두, 돌진은 판정 구가 지나간 적만(구간이 끝나면 남은 적 모두)
    private void UpdateClip(float time, in AttackArea area)
    {
        bool isActive = m_Hitter.IsActive(Spec, time);
        if (IsCharge)
            Owner.MoveF.SetVelocity(isActive ? Owner.transform.forward * Spec.ActiveMoveSpeed : Vector3.zero);

        if (isActive == false)
        {
            if (IsCharge && m_Hitter.IsTicked)
                m_DodgeNotifier.Resolve(area, Spec, true);

            return;
        }

        bool isFirstTick = m_Hitter.Tick(Spec, time, area);
        if (IsCharge)
            m_DodgeNotifier.Resolve(area, Spec, false);
        else if (isFirstTick)
            m_DodgeNotifier.Resolve(area, Spec, true);
    }
}
