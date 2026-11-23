using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 공격 하나. 예고(AttackSpec.WindupTime)가 있으면 먼저 움츠리며 대상 쪽으로 돌고, 그 뒤 클립·돌진·판정을 돌린다.
// 몬스터 공격은 예고가 없어도(클립 자체 예비 동작) 시작부터 판정 시작까지 바닥 예고를 그린다.
// 퍼펙트 닷지: 몬스터 공격은 판정이 닿기 전까지 범위에 들어온 적과 그때 판정까지 남은 시간을 적어 두고, 판정 때 안 맞은 적에게 알린다(HitFeature.TryPerfectDodge).
// 일반 공격은 첫 판정 때 한 번, 돌진은 남은 경로(바닥 예고 사각형)로 보고 돌진이 그 대상 자리를 지나갈 때(또는 판정 구간이 끝날 때) 대상마다 본다
// 판정: 판정 구간(HitTime~HitEndTime) 동안 공격 구체와 후보 유닛의 몸(HitFeature 모양)을 비교한다. 한 공격(한 회차)에서 맞은 대상은 다시 맞지 않는다.
// 클립이 끝나면 끝. 판정 구간이 지나면 대시로, 거기서 캔슬 지연까지 지나면 다음 공격으로 끊을 수 있다(피격·사망은 언제나)

public class AttackAction : UnitAction
{
    private static readonly Vector3 s_SquashScale = new(1.15f, 0.8f, 1.15f); // 예고 끝 움츠림 배율
    private readonly float m_BlendTime;    // 공격 애니로 넘어갈 때 섞는 시간(UnitDef.AttackBlendTime)
    private readonly List<UnitHitCandidate> m_ListCandidate = new();
    private readonly HashSet<UnitController> m_SetHit = new(); // 이번 회차에 이미 맞은 대상
    private readonly Dictionary<UnitController, float> m_DictThreat = new(); // 판정 전 범위에 들어온 적 → 들어와 있던 가장 늦은 시점의 판정까지 남은 시간
    private readonly HashSet<UnitController> m_SetResolved = new(); // 이번 공격에서 퍼펙트 닷지 여부를 이미 정한 적(맞았거나 물었거나). 다시 적지 않는다
    private readonly List<UnitController> m_ListResolve = new(); // 돌진 중 이번 프레임에 정할 적(사전 순회 중 지우지 않으려고)
    
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
    private bool IsCharge => Spec.ActiveMoveSpeed > 0f; // 판정 구간 동안 앞으로 나아가는 돌진
    private float HitAt => Spec.WindupTime + Spec.HitTime / Owner.StatF.GetRate(EStatType.AttackSpeed); // 시작부터 첫 판정까지(판정은 공격 속도만큼 당겨진다)
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
        m_DictThreat.Clear();
        m_SetResolved.Clear();
        
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
        UpdateThreat();

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

        float total = HitAt;
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

    // 몬스터 공격만: 판정이 닿기 전까지 범위에 든 적을 적어 둔다. 남은 시간은 들어와 있던 가장 늦은 시점 것(판정 직전까지 있었을수록 작다)
    // 일반 공격은 첫 판정 전까지 판정 구로, 돌진은 판정 구간 끝까지 남은 돌진 경로로 본다
    private void UpdateThreat()
    {
        if (Owner.Team == ETeam.Player)
            return;

        if (m_IsTicked && (IsCharge == false || AttackTime > Spec.HitEndTime))
            return;

        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null)
            return;

        float toHitStart = Mathf.Max(0f, HitAt - Time);
        Vector3 center = GetCenter(Owner.transform, Spec, Owner.Property.GetBattleBone(Spec.HitPart));
        float radius = Radius;

        if (IsCharge == false)
        {
            units.CollectNearbyEnemies(center, radius, Owner.Team, m_ListCandidate);
            foreach (UnitHitCandidate candidate in m_ListCandidate)
            {
                if (m_SetResolved.Contains(candidate.Unit) == false && candidate.OverlapsSphere(center, radius, out _, out _))
                    m_DictThreat[candidate.Unit] = toHitStart;
            }

            m_ListCandidate.Clear();
            return;
        }

        Vector3 forward = GetFlatForward();
        float travel = GetChargeTravel();
        units.CollectNearbyEnemies(center + forward * (travel * 0.5f), radius + travel * 0.5f, Owner.Team, m_ListCandidate);
        
        foreach (UnitHitCandidate candidate in m_ListCandidate)
        {
            if (m_SetResolved.Contains(candidate.Unit))
                continue;

            if (TryGetChargeDistance(candidate.Unit, center, forward, radius, travel, out float distance))
                m_DictThreat[candidate.Unit] = toHitStart + Mathf.Max(0f, distance) / Spec.ActiveMoveSpeed;
        }

        m_ListCandidate.Clear();
    }

    // 판정 구간에서 아직 나아갈 돌진 거리
    private float GetChargeTravel()
    {
        float clipTime = Mathf.Max(AttackTime, Spec.HitTime);
        return Spec.ActiveMoveSpeed * Mathf.Max(0f, Spec.HitEndTime - clipTime) / Owner.StatF.GetRate(EStatType.AttackSpeed);
    }

    private Vector3 GetFlatForward()
    {
        Vector3 forward = Owner.transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    // 돌진 경로(판정 구가 travel만큼 쓸고 가는 자리) 위에 대상 몸이 있으면 true. distance는 판정 구가 몸에 닿기까지 더 가야 하는 거리(0 이하면 이미 닿는 자리)
    private static bool TryGetChargeDistance(UnitController unit, Vector3 center, Vector3 forward, float radius, float travel, out float distance)
    {
        float reach = radius + unit.HurtBody.radius * unit.transform.lossyScale.x;
        Vector3 offset = unit.transform.position - center;
        offset.y = 0f;

        float along = Vector3.Dot(offset, forward);
        Vector3 side = offset - forward * along;
        distance = along - reach;

        return side.sqrMagnitude <= reach * reach && along >= -reach && along <= travel + reach;
    }

    // 돌진 중 매 판정 뒤: 판정 구 중심이 지나간 대상만 정한다. isEnd면 남은 대상 모두
    private void ResolveCharge(bool isEnd)
    {
        Vector3 center = GetCenter(Owner.transform, Spec, Owner.Property.GetBattleBone(Spec.HitPart));
        Vector3 forward = GetFlatForward();

        foreach (UnitController unit in m_DictThreat.Keys)
        {
            Vector3 offset = unit.transform.position - center;
            offset.y = 0f;
            
            if (isEnd || Vector3.Dot(offset, forward) <= 0f)
                m_ListResolve.Add(unit);
        }

        foreach (UnitController unit in m_ListResolve)
        {
            Resolve(unit, m_DictThreat[unit]);
            m_DictThreat.Remove(unit);
        }

        m_ListResolve.Clear();
    }

    // 일반 공격 첫 판정 직후: 범위에 들어왔었는데 안 맞은 적 모두에게 묻는다
    private void CheckPerfectDodge()
    {
        foreach (KeyValuePair<UnitController, float> pair in m_DictThreat)
        {
            Resolve(pair.Key, pair.Value);
        }

        m_DictThreat.Clear();
    }

    // 안 맞은 적에게 퍼펙트 닷지 여부를 묻는다(대시·창 검사는 HitFeature). 맞은 적은 PerformHit에서 이미 빠졌다
    private void Resolve(UnitController unit, float threatLead)
    {
        m_SetResolved.Add(unit);

        Vector3 direction = unit.transform.position - Owner.transform.position;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Owner.transform.forward;

        HitParams hit = new(Owner, Owner.Team, Spec, direction, unit.transform.position, -1);
        unit.HitF.TryPerfectDodge(hit, threatLead);
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
        {
            // 돌진이 끝났는데 아직 안 지나간 대상도 정한다
            if (IsCharge && m_IsTicked && m_DictThreat.Count > 0)
                ResolveCharge(true);

            return;
        }

        bool isFirstTick = m_IsTicked == false;
        if (isFirstTick)
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

        if (IsCharge)
            ResolveCharge(false);
        else if (isFirstTick)
            CheckPerfectDodge();
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
            {
                m_SetHit.Add(candidate.Unit);
                m_SetResolved.Add(candidate.Unit); // 맞았으니 퍼펙트 닷지 아님
                m_DictThreat.Remove(candidate.Unit);
            }
        }

        m_ListCandidate.Clear();
    }
}
