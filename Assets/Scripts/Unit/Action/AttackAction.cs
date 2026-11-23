using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 공격 하나. 예고(AttackSpec.WindupTime)가 있으면 먼저 움츠리며 대상 쪽으로 돌고, 그 뒤 클립·돌진·판정을 돌린다.
// 몬스터 공격은 예고가 없어도(클립 자체 예비 동작) 시작부터 판정 시작까지 바닥 예고를 그린다.
// 판정: 판정 구간(HitTime~HitEndTime) 동안 공격 구체와 후보 유닛의 몸(HitFeature 모양)을 비교한다. 한 공격(한 회차)에서 맞은 대상은 다시 맞지 않는다.
// 클립이 끝나면 끝. 판정 구간이 지나면 대시로, 거기서 캔슬 지연까지 지나면 다음 공격으로 끊을 수 있다(피격·사망은 언제나)

public class AttackAction : UnitAction
{
    private static readonly Vector3 s_SquashScale = new(1.15f, 0.8f, 1.15f); // 예고 끝 움츠림 배율
    private readonly float m_BlendTime;    // 공격 애니로 넘어갈 때 섞는 시간(UnitDef.AttackBlendTime)
    private readonly List<UnitHitCandidate> m_ListCandidate = new();
    private readonly HashSet<UnitController> m_SetHit = new(); // 이번 회차에 이미 맞은 대상
    
    private Vector3 m_Direction;
    private Transform m_FaceTarget;
    private bool m_IsStarted;      // 예고가 끝나고 클립을 틀었는지
    private Vector3 m_BaseScale;   // 움츠리기 전 모델 크기
    private float m_Length;        // 지금 공격 클립 길이(초)
    private bool m_IsTicked;       // 판정을 한 번이라도 했는지(한 프레임 판정 보장)
    private int m_HitRound;        // 다시 맞히기 회차(AttackSpec.RehitFrame). 바뀌면 맞은 대상 목록을 비운다
    private TelegraphController m_Telegraph; // 바닥 위험 범위. 몬스터 공격만, 판정 시작까지
    
    public AttackSpec Spec { get; private set; }  // 지금(끝난 뒤엔 마지막) 공격. 콤보 다음 타를 여기서 읽는다
    public float CancelDelay { get; set; }  // 판정 뒤 이 시간이 지나면 다음 공격으로 끊을 수 있다. 0이면 판정 끝나자마자

    public override EActionType Type => EActionType.Attack;
    public override bool IsFinished => m_IsStarted && AttackTime >= m_Length;
    public bool CanBufferNext => m_IsStarted && AttackTime >= Spec.HitTime; // 판정 시작 뒤에 누른 것만 다음 타로 받는다(너무 이른 연타는 무시)
    public float AttackTime => (Time - Spec.WindupTime) * Owner.StatF.GetRate(EStatType.AttackSpeed); // 예고가 끝나고 클립이 시작된 뒤 지난 시간(공격 속도만큼 빠르게 흐른다)
    private float Radius => DamageCalculator.GetRadius(Owner, Spec); // 판정 반지름(공격 강화 반영)
    private Transform VisualRoot => Owner.Property.Animator.transform;

    // ------------------------------------------------

    public AttackAction(UnitController owner, UnitDef def) : base(owner)
    {
        m_BlendTime = def.AttackBlendTime;

        // 판정 부위 뼈가 프리팹에 없으면 몸 앞으로 판정한다
        List<AttackSpec> listAttack = new();
        def.CollectAttacks(listAttack);
        
        foreach (AttackSpec attack in listAttack)
        {
            if (attack.HitPart != EBattlePartType.None && owner.Property.GetBattleBone(attack.HitPart) == null)
                CLogger.Warning("AttackAction", $"판정 부위 뼈가 프리팹에 없음: {attack.HitPart} ({owner.transform.name})", owner.transform);
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
        EndHit();
    }

    public override bool CanInterruptBy(EActionType next)
    {
        switch (next)
        {
            case EActionType.Attack:
                return m_IsStarted && IsHitDone() && AttackTime >= Spec.HitEndTime + CancelDelay;
            case EActionType.Dash:
                return m_IsStarted && IsHitDone();
            default:
                return base.CanInterruptBy(next);
        }
    }

    protected override void Enter()
    {
        m_IsStarted = false;
        
        if (m_Direction.sqrMagnitude > 0.0001f)
            Owner.MoveF.FaceTo(m_Direction, true);

        // 바닥 예고는 플레이어가 피할 공격(몬스터)만 그린다
        if (Owner.Team != ETeam.Player)
            m_Telegraph = FlowManager.Instance.EffectS?.ShowTelegraph(Spec.ActiveMoveSpeed > 0f);

        if (Spec.WindupTime <= 0f)
        {
            StartClip();
            UpdateTelegraph();
            
            return;
        }

        // 예고: 멈춰서 움츠린다. 방향은 TurnSpeed로만 맞추고, 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        Owner.MoveF.Stop();
        Owner.AnimF.Play(AnimKey.Idle);
        
        m_BaseScale = VisualRoot.localScale;
        UpdateTelegraph();
    }

    protected override void Update(float deltaTime)
    {
        UpdateTelegraph();

        if (m_IsStarted)
        {
            UpdateClip(AttackTime);
            return;
        }

        if (m_FaceTarget != null)
            Owner.MoveF.FaceTo(m_FaceTarget.position - Owner.transform.position);

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
        
        EndHit();
        HideTelegraph();
    }

    // ==================================================

    // 예고 시작부터 판정 시작까지 판정 자리를 바닥에 그린다. 예고 중 대상 쪽으로 도니 매 프레임 따라간다.
    // 돌진(ActiveMoveSpeed)은 판정 구간 동안 나아가는 거리만큼 긴 사각형
    // shortcut: 시작 돌진(Lunge)으로 밀리는 거리와 판정 뼈(HitPart)의 공격 자세 위치는 반영 안 함, 범위가 어긋나 보이면 그때 더한다
    private void UpdateTelegraph()
    {
        if (m_Telegraph == null)
            return;

        float total = Spec.WindupTime + Spec.HitTime / Owner.StatF.GetRate(EStatType.AttackSpeed); // 판정은 공격 속도만큼 당겨진다(AttackTime)
        if (Time >= total)
        {
            HideTelegraph();
            return;
        }

        Transform owner = Owner.transform;
        Vector3 center = GetCenter(owner, Spec, Owner.Property.GetBattleBone(Spec.HitPart));
        center.y = owner.position.y;

        float diameter = Radius * 2f;
        if (m_Telegraph.IsLine)
        {
            float length = diameter + Spec.ActiveMoveSpeed * Mathf.Max(0f, Spec.HitEndTime - Spec.HitTime);
            m_Telegraph.SetShape(center - owner.forward * Radius, owner.forward, diameter, length);
        }
        else
        {
            m_Telegraph.SetShape(center, owner.forward, diameter, diameter);
        }

        m_Telegraph.SetProgress(Time / total);
    }

    private void HideTelegraph()
    {
        FlowManager.Instance.EffectS?.HideTelegraph(m_Telegraph);
        m_Telegraph = null;
    }

    // 클립 시작: 판정 걸기, 멈춰서 클립 재생, 시작 돌진(Lunge). 방향은 Enter에서 먼저 맞춘다
    private void StartClip()
    {
        m_IsStarted = true;
        m_IsTicked = false;
        m_HitRound = 0;
        m_SetHit.Clear();
        m_Length = Owner.AnimF.GetLength(Spec.Clip);

        Owner.MoveF.Stop();
        Owner.AnimF.Play(Spec.Clip, true, m_BlendTime);
        Owner.AnimF.SetSpeed(Owner.StatF.GetRate(EStatType.AttackSpeed));

        if (Spec.Lunge > 0f)
            Owner.MoveF.AddImpulse(Owner.transform.forward * Spec.Lunge);
    }

    // 클립 중 매 프레임(time = 클립 시작부터 지난 시간): 판정 구간 동안 앞으로 돌진하고 판정한다
    private void UpdateClip(float time)
    {
        if (Spec.ActiveMoveSpeed > 0f)
            Owner.MoveF.SetVelocity(IsHitActive(time) ? Owner.transform.forward * Spec.ActiveMoveSpeed : Vector3.zero);

        if (IsHitActive(time) == false)
            return;

        if (m_IsTicked == false)
            PlayEffect();

        m_IsTicked = true;
        if (Spec.RehitTime > 0f)
        {
            int round = Mathf.FloorToInt((time - Spec.HitTime) / Spec.RehitTime);
            if (round != m_HitRound)
            {
                m_HitRound = round;
                m_SetHit.Clear();
            }
        }

        PerformHit();
    }

    private void EndHit()
    {
        m_IsTicked = false;
        m_SetHit.Clear();
    }

    // 판정 구간 안인지. 끝 프레임이 시작 이하면 첫 판정 한 프레임만
    private bool IsHitActive(float time)
    {
        if (time < Spec.HitTime)
            return false;

        return m_IsTicked == false || time <= Spec.HitEndTime;
    }

    // 판정 구간이 지났는지
    private bool IsHitDone()
    {
        return m_IsTicked && AttackTime > Spec.HitEndTime;
    }

    // 판정 자리(땅 이펙트는 발밑)에 주인 방향으로 터뜨리고 판정 반지름만큼 키운다
    private void PlayEffect()
    {
        EffectService effectS = FlowManager.Instance.EffectS;
        if (Spec.Effect == null || effectS == null)
            return;

        Transform owner = Owner.transform;
        Vector3 position = GetCenter(owner, Spec, Owner.Property.GetBattleBone(Spec.HitPart));
        
        if (Spec.Effect.IsOnGround)
            position.y = owner.position.y;

        effectS.Play(Spec.Effect, position, owner.rotation, Radius);
    }

    // 판정 구와 겹친 적에게 타격을 넣는다. 무적 등으로 막히면 구간 안에서 다시 노린다
    private void PerformHit()
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null)
            return;

        Transform owner = Owner.transform;
        Vector3 center = GetCenter(owner, Spec, Owner.Property.GetBattleBone(Spec.HitPart));
        ETeam team = Owner.Team;
        float radius = Radius;
        
        units.CollectNearbyEnemies(center, radius, team, m_ListCandidate);

        foreach (UnitHitCandidate candidate in m_ListCandidate)
        {
            if (m_SetHit.Contains(candidate.Unit))
                continue;

            CharacterController controller = candidate.Unit.HurtBody;
            if (controller == null || controller.enabled == false || controller.gameObject.activeInHierarchy == false)
                continue;

            if (candidate.OverlapsSphere(center, radius, out Vector3 point, out int partIndex) == false)
                continue;

            Vector3 direction = candidate.Unit.transform.position - owner.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : owner.forward;

            HitParams hit = new(Owner, team, Spec, direction, point, partIndex);
            if (candidate.Unit.HitF.Receive(hit))
                m_SetHit.Add(candidate.Unit);
        }

        m_ListCandidate.Clear();
    }
}
