using System.Collections.Generic;
using UnityEngine;

// ==================================================

public enum EMonsterState
{
    Idle,
    Wander,
    Chase,
    Windup,
    Attack,
    Hit,
    Flee,
    Dead,
}

// ==================================================

// 몬스터 하나의 두뇌. 몸(UnitController)에 FSM과 몸에 꽂는 버섯을 더한다.
// 상태 클래스는 State/ 폴더(상태마다 파일 하나), 수치는 MonsterDef
public class MonsterController : UnitController<MonsterDef>
{
    private readonly StateMachine<EMonsterState> m_Fsm = new();
    private readonly List<AttackSpec> m_ListAttackChoice = new(); // 고를 수 있는 공격: Def.ListSkill
    private readonly List<float> m_ListAttackCooldown = new();  // m_ListAttackChoice 칸마다 남은 개별 쿨
    private const float PartDropUpSpeed = 5f;     // 부위 버섯이 튀어 오르는 속도
    private const float PartDropSpread = 2f;      // 부위 버섯이 옆으로 퍼지는 속도

    public Transform Target { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastHitDirection { get; private set; }
    public float CooldownRemain { get; set; }                  // 공용 쿨
    public AttackSpec CurrentAttack { get; private set; }      // 추적에서 고른, 이번에 쓸 공격
    
    public EMonsterState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;
    public PartFeature Parts { get; private set; }             // 몸에 꽂힌 버섯 부위
    protected override ETeam Team => ETeam.Monster;

    // ------------------------------------------------

    protected override void Set()
    {
        base.Set();
        if (enabled == false)
            return;

        AttachMushrooms();
        AddBodyShapes();

        Def.CollectAttacks(m_ListAttackChoice);
        for (int i = 0; i < m_ListAttackChoice.Count; i++)
        {
            m_ListAttackCooldown.Add(0f);
        }

        m_Fsm.Add(EMonsterState.Idle, new MonsterIdleState(this));
        m_Fsm.Add(EMonsterState.Wander, new MonsterWanderState(this));
        m_Fsm.Add(EMonsterState.Chase, new MonsterChaseState(this));
        m_Fsm.Add(EMonsterState.Windup, new MonsterWindupState(this));
        m_Fsm.Add(EMonsterState.Attack, new MonsterAttackState(this));
        m_Fsm.Add(EMonsterState.Hit, new MonsterHitState(this));
        m_Fsm.Add(EMonsterState.Flee, new MonsterFleeState(this));
        m_Fsm.Add(EMonsterState.Dead, new MonsterDeadState(this));
    }

    protected override void OnSpawned()
    {
        HomePosition = transform.position;
        Target = null;
        LastHitDirection = Vector3.zero;
        CooldownRemain = 0f;
        CurrentAttack = null;

        for (int i = 0; i < m_ListAttackCooldown.Count; i++)
        {
            m_ListAttackCooldown[i] = 0f;
        }

        m_Fsm.Change(EMonsterState.Idle);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (CooldownRemain > 0f)
            CooldownRemain -= deltaTime;

        for (int i = 0; i < m_ListAttackCooldown.Count; i++)
        {
            if (m_ListAttackCooldown[i] > 0f)
                m_ListAttackCooldown[i] -= deltaTime;
        }

        PlayerController player = GetValidPlayer();
        if (Target == null && player != null)
            Target = player.transform;

        m_Fsm.Update(deltaTime);
    }
    
    // 진행 중인 상태를 Exit로 정리(움츠림 크기·애니 배속 복구)하고 AI를 끈다
    protected override void OnBattleEnded()
    {
        m_Fsm.Stop();
        base.OnBattleEnded();
    }

    protected override void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        LastHitDirection = hit.Direction;
        if (hit.Attacker != null)
            Target = hit.Attacker.transform;

        // [mj] todo) 더 나은 대안...? 
        m_Fsm.Change(EMonsterState.Hit);
    }

    protected override void OnDied()
    {
        m_Fsm.Change(EMonsterState.Dead);
    }
    
    // ---
    
    public void ChangeState(EMonsterState next)
    {
        m_Fsm.Change(next);
    }

    // 죽은 뒤 사라질 때. 등록돼 있으면 ActorService가 지운다
    public void Despawn()
    {
        ActorService actors = GameManager.Instance?.ActorS;
        if (actors != null && ActorID != 0)
        {
            actors.Despawn(ActorID);
            return;
        }

        Destroy(gameObject);
    }

    public void MoveTo(Vector3 direction, float speed)
    {
        direction.y = 0f;
        Move.SetVelocity(direction.normalized * speed);
        Move.FaceTo(direction);
    }

    // attack ---
    
    // 공용 쿨이 돌았으면 개별 쿨이 돌고 거리가 맞는 공격(기본 + 스킬) 중 가중치로 하나 골라 CurrentAttack에 둔다
    public bool TryPickAttack(float distance)
    {
        if (CooldownRemain > 0f)
            return false;

        IReadOnlyList<AttackSpec> listAttack = m_ListAttackChoice;
        float totalWeight = 0f;
        
        for (int i = 0; i < listAttack.Count; i++)
        {
            if (CanUseAttack(i, distance))
                totalWeight += listAttack[i].Weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        
        for (int i = 0; i < listAttack.Count; i++)
        {
            if (CanUseAttack(i, distance) == false)
                continue;

            CurrentAttack = listAttack[i];

            roll -= CurrentAttack.Weight;
            if (roll <= 0f)
                break;
        }

        return true;
    }

    // 공격이 끝날 때. 공용 쿨과 쓴 공격의 개별 쿨을 건다
    public void StartAttackCooldown()
    {
        CooldownRemain = Def.AttackCooldown;

        int index = m_ListAttackChoice.IndexOf(CurrentAttack);
        if (index >= 0)
            m_ListAttackCooldown[index] = CurrentAttack.Cooldown;
    }

    // part ---

    // 몸 판정 표의 부위 뼈로 판정 모양을 등록한다. 버섯 칸이 걸린 부위는 그 칸 번호를 달아 부위 판정이 된다
    private void AddBodyShapes()
    {
        foreach (HitShapeDef shapeDef in Def.ListBodyShape)
        {
            Transform bone = GetBattleBone(shapeDef.Part);
            if (bone == null)
            {
                CLogger.Warning("MonsterController", $"판정 부위 뼈가 프리팹에 없음: {shapeDef.Part} ({name})", this);
                continue;
            }

            AddHitShape(new HitShape(bone, shapeDef, FindPartIndex(shapeDef.Part)));
        }
    }

    // 버섯 부위. 스폰 때 PartFeature가 소켓을 골라 꽂는다
    private void AttachMushrooms()
    {
        Parts = AddFeature(new PartFeature(this, Def.ListPart));
        Parts.Broken += OnPartBroken;
        HitFeature.SetParts(Parts);
    }

    // 살아 있을 때 깬 버섯은 상급으로 확정 드랍. 꽂힌 버섯 하나마다 하나. 휘청·다운 전환은 보스 상태(6번)에서 붙인다
    private void OnPartBroken(PartFeature.Part part)
    {
        if (part.Entry.Drop == null)
            return;

        for (int i = 0; i < part.PlacedCount; i++)
        {
            Vector3 position = part.ListMushroom[i].Socket.position;
            MushroomPickup pickup = ResourceManager.Instance.Spawn(part.Entry.Drop, position, Quaternion.identity);
            Vector2 spread = Random.insideUnitCircle * PartDropSpread;
            pickup.Launch(new Vector3(spread.x, PartDropUpSpeed, spread.y), IsDead ? EFreshness.Low : EFreshness.High);
        }
    }

    #region Gizmo
    
    // 씬 뷰에 몸 판정 모양(구·캡슐)과 부위 판정 구를 그린다. 선이 뼈 축 방향
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (Def == null)
            return;

        DrawPartGizmos();

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        foreach (HitShapeDef shapeDef in Def.ListBodyShape)
        {
            Transform bone = GetBattleBone(shapeDef.Part);
            if (bone == null)
                continue;

            new HitShape(bone, shapeDef, -1).GetSegment(out Vector3 start, out Vector3 end);
            Gizmos.DrawWireSphere(start, shapeDef.Radius);
            Gizmos.DrawWireSphere(end, shapeDef.Radius);
            Gizmos.DrawLine(start, end);
        }
    }
    
    // 깨지는 부위의 몸 판정 모양 위에 상태색(초록 멀쩡, 빨강 깨짐)과 남은 내구도를 그린다
    private void DrawPartGizmos()
    {
        IReadOnlyList<MonsterDef.PartEntry> listEntry = Def.ListPart;
        for (int i = 0; i < listEntry.Count; i++)
        {
            MonsterDef.PartEntry entry = listEntry[i];
            if (entry.IsBreakable == false)
                continue;

            Transform bone = GetBattleBone(entry.BattlePart);
            HitShapeDef shapeDef = FindBodyShape(entry.BattlePart);
            if (bone == null || shapeDef == null)
                continue;

            PartFeature.Part part = Parts?.GetPart(i);
            Gizmos.color = part != null && part.IsBroken ? Color.red : Color.green;
            new HitShape(bone, shapeDef, i).GetSegment(out Vector3 start, out Vector3 end);
            Gizmos.DrawWireSphere(start, shapeDef.Radius);
            Gizmos.DrawWireSphere(end, shapeDef.Radius);
            Gizmos.DrawLine(start, end);

#if UNITY_EDITOR
            string durability = part != null ? $"{part.Durability}/{entry.Durability}" : $"{entry.Durability}";
            UnityEditor.Handles.Label(end + Vector3.up * shapeDef.Radius, $"{i}:{entry.BattlePart} {durability}");
#endif
        }
    }

    #endregion
    
    # region Get

    // target ---
    
    public Vector3 GetTargetDirection()
    {
        if (Target == null)
            return transform.forward;

        Vector3 diff = Target.position - transform.position;
        diff.y = 0f;
        return diff.sqrMagnitude > 0.0001f ? diff.normalized : transform.forward;
    }
    
    public float GetTargetDistance()
    {
        if (Target == null)
            return float.MaxValue;

        // 내 캡슐 표면부터 잰다. 피벗 기준이면 반지름이 MaxRange보다 큰 몬스터(보스)가 공격 거리에 못 들어온다
        Vector3 diff = Target.position - transform.position;
        diff.y = 0f;
        float bodyRadius = HurtBody != null ? HurtBody.radius : 0f;
        return Mathf.Max(0f, diff.magnitude - bodyRadius);
    }

    public bool CanSeeTarget()
    {
        if (Target == null || GetValidPlayer() == null)
            return false;

        return GetTargetDistance() <= Def.SightRange;
    }

    // attack ---
    
    // 노릴 수 있는 플레이어. 없거나 PlayerController.IsValid()가 false면 null
    public PlayerController GetValidPlayer()
    {
        PlayerController player = GameManager.Instance?.ActorS?.Player;
        if (player == null || player.IsValid() == false)
            return null;

        return player;
    }
    
    private bool CanUseAttack(int index, float distance)
    {
        AttackSpec attack = m_ListAttackChoice[index];
        if (attack.Weight <= 0f || m_ListAttackCooldown[index] > 0f)
            return false;

        return distance >= attack.MinRange && distance <= attack.MaxRange;
    }

    // part ---
    
    // 이 몸 부위를 BattlePart로 가진 깨지는 부위 칸. 없으면 -1. 같은 부위 칸이 둘이면 첫 칸
    private int FindPartIndex(EBattlePartType battlePart)
    {
        if (battlePart == EBattlePartType.None)
            return -1;

        IReadOnlyList<MonsterDef.PartEntry> listEntry = Def.ListPart;
        for (int i = 0; i < listEntry.Count; i++)
        {
            if (listEntry[i].BattlePart == battlePart && listEntry[i].IsBreakable)
                return i;
        }

        return -1;
    }
    
    private HitShapeDef FindBodyShape(EBattlePartType battlePart)
    {
        foreach (HitShapeDef shapeDef in Def.ListBodyShape)
        {
            if (shapeDef.Part == battlePart)
                return shapeDef;
        }

        return null;
    }
    
    #endregion
}
