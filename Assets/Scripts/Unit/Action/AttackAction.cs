using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 공격 하나. 예고(AttackSpec.WindupTime)가 있으면 먼저 움츠리며 대상 쪽으로 돌고, 그 뒤 클립·돌진·판정을 돌린다.
// 판정: 판정 구간(HitTime~HitEndTime) 동안 공격 구체와 후보 유닛의 몸(HitFeature 모양)을 비교한다. 한 공격(한 회차)에서 맞은 대상은 다시 맞지 않는다.
// 클립이 끝나면 끝. 판정 구간이 지나면 대시로, 거기서 캔슬 지연까지 지나면 다음 공격으로 끊을 수 있다(피격·사망은 언제나)
// 몬스터 공격(플레이어가 피할 공격)은 시작부터 판정 시작까지 바닥 예고를 그리고, 퍼펙트 닷지를 판정한다(PerfectDodgeTracker).
// 범위는 매 프레임 AttackArea 하나로 계산해 예고·판정 효과·퍼펙트 닷지가 같이 쓴다
// 시간: Time은 실제로 흐른 시간, AttackTime은 클립 시간(공격 속도가 곱해짐). 클립 시간을 실제 시간으로 바꿀 땐 ToRealTime

public class AttackAction : UnitAction
{
    private static readonly Vector3 s_SquashScale = new(1.15f, 0.8f, 1.15f); // 예고 끝 움츠림 배율
    private readonly float m_BlendTime;    // 공격 애니로 넘어갈 때 섞는 시간(UnitDef.AttackBlendTime)
    private readonly List<UnitHitCandidate> m_ListCandidate = new();
    private readonly HashSet<UnitController> m_SetHit = new(); // 이번 회차에 이미 맞은 대상
    private readonly PerfectDodgeTracker m_DodgeTracker;       // 몬스터 공격만 쓴다
    
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
    public float AttackTime => (Time - Spec.WindupTime) * AttackRate; // 예고가 끝나고 클립이 시작된 뒤 지난 클립 시간
    private float AttackRate => Owner.StatF.GetRate(EStatType.AttackSpeed); // 공격 속도 배율. 클립·판정이 이만큼 빨라진다
    private float HitAt => Spec.WindupTime + ToRealTime(Spec.HitTime); // 시작부터 첫 판정까지 실제 시간
    private float Radius => DamageCalculator.GetRadius(Owner, Spec); // 판정 반지름(공격 강화 반영)
    private bool IsCharge => Spec.ActiveMoveSpeed > 0f; // 판정 구간 동안 앞으로 나아가는 돌진
    private bool IsMonsterAttack => Owner.Team != ETeam.Player; // 플레이어가 피할 공격(바닥 예고·퍼펙트 닷지)
    private Transform VisualRoot => Owner.Property.Animator.transform;

    // ------------------------------------------------

    public AttackAction(UnitController owner, UnitDef def) : base(owner)
    {
        m_BlendTime = def.AttackBlendTime;
        m_DodgeTracker = new PerfectDodgeTracker(owner);

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
        m_DodgeTracker.Clear();
        
        if (m_Direction.sqrMagnitude > 0.0001f)
            Owner.MoveF.FaceTo(m_Direction, true);

        if (IsMonsterAttack)
            m_Telegraph = FlowManager.Instance.EffectS?.ShowTelegraph(IsCharge);

        if (Spec.WindupTime <= 0f)
        {
            StartClip();
            UpdateTelegraph(GetArea());
            
            return;
        }

        // 예고: 멈춰서 움츠린다. 방향은 TurnSpeed로만 맞추고, 공격 중에는 고정해서 옆으로 빠질 틈을 준다
        Owner.MoveF.Stop();
        Owner.AnimF.Play(AnimKey.Idle);
        
        m_BaseScale = VisualRoot.localScale;
        UpdateTelegraph(GetArea());
    }

    protected override void Update(float deltaTime)
    {
        AttackArea area = GetArea();
        UpdateTelegraph(area);
        TrackDodge(area);

        if (m_IsStarted)
        {
            UpdateClip(AttackTime, area);
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
    
    // ------------------------------------------------

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

        return new AttackArea(center, owner.position.y, owner.forward, Radius, length);
    }

    #region 바닥 예고

    // 예고 시작부터 판정 시작까지 범위를 바닥에 그린다. 예고 중 대상 쪽으로 도니 매 프레임 따라간다. 꽉 차는 순간 판정
    // shortcut: 시작 돌진(Lunge)으로 밀리는 거리와 판정 뼈(HitPart)의 공격 자세 위치는 반영 안 함, 범위가 어긋나 보이면 그때 더한다
    private void UpdateTelegraph(in AttackArea area)
    {
        if (m_Telegraph == null)
            return;

        float total = HitAt;
        if (Time >= total)
        {
            HideTelegraph();
            return;
        }

        float diameter = area.Radius * 2f;
        if (m_Telegraph.IsLine)
            m_Telegraph.SetShape(area.Ground - area.Forward * area.Radius, area.Forward, diameter, diameter + area.Length);
        else
            m_Telegraph.SetShape(area.Ground, area.Forward, diameter, diameter);

        m_Telegraph.SetProgress(Time / total);
    }

    private void HideTelegraph()
    {
        FlowManager.Instance.EffectS?.HideTelegraph(m_Telegraph);
        m_Telegraph = null;
    }

    #endregion

    #region 퍼펙트 닷지

    // 판정이 닿기 전까지 범위에 든 적을 적어 둔다. 일반 공격은 첫 판정 전까지, 돌진은 판정 구간 끝까지
    private void TrackDodge(in AttackArea area)
    {
        if (IsMonsterAttack == false)
            return;

        if (m_IsTicked && (IsCharge == false || AttackTime > Spec.HitEndTime))
            return;

        m_DodgeTracker.Track(area, Mathf.Max(0f, HitAt - Time), Spec.ActiveMoveSpeed);
    }

    // 판정 뒤에 안 맞은 적에게 묻는다. 일반 공격은 첫 판정 때 모두, 돌진은 판정 구가 지나간 적만(isEnd면 남은 적 모두)
    private void ResolveDodge(in AttackArea area, bool isFirstTick, bool isEnd)
    {
        if (IsMonsterAttack == false)
            return;

        if (IsCharge)
            m_DodgeTracker.Resolve(area, Spec, isEnd);
        else if (isFirstTick)
            m_DodgeTracker.Resolve(area, Spec, true);
    }

    #endregion

    #region 클립·판정

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
        Owner.AnimF.SetSpeed(AttackRate);

        if (Spec.Lunge > 0f)
            Owner.MoveF.AddImpulse(Owner.transform.forward * Spec.Lunge);
    }

    // 클립 중 매 프레임(time = 클립 시간): 판정 구간 동안 앞으로 돌진하고 판정한다
    private void UpdateClip(float time, in AttackArea area)
    {
        bool isActive = IsHitActive(time);
        if (IsCharge)
            Owner.MoveF.SetVelocity(isActive ? Owner.transform.forward * Spec.ActiveMoveSpeed : Vector3.zero);

        if (isActive == false)
        {
            // 돌진이 끝났으면 아직 안 지나간 적도 정한다
            if (m_IsTicked)
                ResolveDodge(area, false, true);

            return;
        }

        bool isFirstTick = m_IsTicked == false;
        if (isFirstTick)
            PlayEffect(area);

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

        PerformHit(area);
        ResolveDodge(area, isFirstTick, false);
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
    private void PlayEffect(in AttackArea area)
    {
        EffectService effectS = FlowManager.Instance.EffectS;
        if (Spec.Effect == null || effectS == null)
            return;

        Vector3 position = Spec.Effect.IsOnGround ? area.Ground : area.Center;
        effectS.Play(Spec.Effect, position, Owner.transform.rotation, area.Radius);
    }

    // 판정 구와 겹친 적에게 타격을 넣는다. 무적 등으로 막히면 구간 안에서 다시 노린다
    private void PerformHit(in AttackArea area)
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null)
            return;

        Transform owner = Owner.transform;
        Vector3 center = area.Center;
        float radius = area.Radius;
        ETeam team = Owner.Team;
        
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
            {
                m_SetHit.Add(candidate.Unit);
                m_DodgeTracker.OnHit(candidate.Unit);
            }
        }

        m_ListCandidate.Clear();
    }

    #endregion
}
