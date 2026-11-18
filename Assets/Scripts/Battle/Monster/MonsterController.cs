using System.Collections.Generic;
using UnityEngine;

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

// 몬스터 하나의 두뇌. 몸(UnitController)에 FSM과 몸에 꽂는 버섯을 더한다.
// 상태 클래스는 State/ 폴더(상태마다 파일 하나), 수치는 MonsterDef
public class MonsterController : UnitController<MonsterDef>
{
    private readonly StateMachine<EMonsterState> m_Fsm = new();
    private readonly List<AttackSpec> m_ListAttackChoice = new(); // 고를 수 있는 공격: 기본(Def.ListAttack) + 스킬(Def.ListSkill)
    private readonly List<float> m_ListAttackCooldown = new(); // m_ListAttackChoice 칸마다 남은 개별 쿨
    private readonly List<PartFeature> m_ListPart = new();      // [i] = Def.ListMushroom 칸. 버섯이 없는 칸은 null

    private const float PartDropUpSpeed = 5f;     // 부위 버섯이 튀어 오르는 속도
    private const float PartDropSpread = 2f;      // 부위 버섯이 옆으로 퍼지는 속도

    public Transform Target { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastHitDirection { get; private set; }
    public float CooldownRemain { get; set; }                  // 공용 쿨
    public AttackSpec CurrentAttack { get; private set; }      // 추적에서 고른, 이번에 쓸 공격
    
    public EMonsterState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;
    public IReadOnlyList<PartFeature> ListPart => m_ListPart;
    protected override ETeam Team => ETeam.Monster;

    // ------------------------------------------------

    protected override void Set()
    {
        base.Set();
        if (enabled == false)
            return;

        AttachMushrooms();
        AddBodyShapes();

        m_ListAttackChoice.AddRange(Def.ListAttack);
        foreach (SkillDef skill in Def.ListSkill)
        {
            if (skill != null)
                m_ListAttackChoice.Add(skill.Attack);
        }

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

    public void ChangeState(EMonsterState next)
    {
        m_Fsm.Change(next);
    }

    // 상태들이 같이 쓰는 판단

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

    public Vector3 GetTargetDirection()
    {
        if (Target == null)
            return transform.forward;

        Vector3 diff = Target.position - transform.position;
        diff.y = 0f;
        return diff.sqrMagnitude > 0.0001f ? diff.normalized : transform.forward;
    }

    public bool CanSeeTarget()
    {
        if (Target == null || GetValidPlayer() == null)
            return false;

        return GetTargetDistance() <= Def.SightRange;
    }

    // 노릴 수 있는 플레이어. 없거나 PlayerController.IsValid()가 false면 null
    public PlayerController GetValidPlayer()
    {
        PlayerController player = GameManager.Instance?.Actors?.Player;
        if (player == null || player.IsValid() == false)
            return null;

        return player;
    }

    // 죽은 뒤 사라질 때. 등록돼 있으면 ActorService가 지운다
    public void Despawn()
    {
        ActorService actors = GameManager.Instance?.Actors;
        if (actors != null && ActorID != 0)
        {
            actors.Despawn(ActorID);
            return;
        }

        Destroy(gameObject);
    }

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

    public void MoveTo(Vector3 direction, float speed)
    {
        direction.y = 0f;
        Move.SetVelocity(direction.normalized * speed);
        Move.FaceTo(direction);
    }

    // ==================================================

    private bool CanUseAttack(int index, float distance)
    {
        AttackSpec attack = m_ListAttackChoice[index];
        if (attack.Weight <= 0f || m_ListAttackCooldown[index] > 0f)
            return false;

        return distance >= attack.MinRange && distance <= attack.MaxRange;
    }

    // 몸 판정 표의 부위 뼈로 판정 모양을 등록한다
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

            AddHitShape(new HitShape(bone, shapeDef, -1));
        }
    }

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

    // 버섯을 소켓 뼈에 꽂고 부위로 만든다. 일반·깨진 모델을 둘 다 붙여 두고, 맞은 부위만 번쩍이도록 부위 번호와 함께 렌더러를 넘긴다
    private void AttachMushrooms()
    {
        IReadOnlyList<MonsterDef.MushroomSocket> listSocket = Def.ListMushroom;
        for (int i = 0; i < listSocket.Count; i++)
        {
            m_ListPart.Add(CreatePart(i, listSocket[i]));
        }

        HitFeature.SetParts(m_ListPart);
    }

    private PartFeature CreatePart(int index, MonsterDef.MushroomSocket socket)
    {
        if (socket.Model == null)
            return null;

        Transform bone = GetMushroomBone(socket.Part);
        if (bone == null)
        {
            CLogger.Warning("MonsterController", $"버섯 자리 뼈가 프리팹에 없음: {socket.Part} ({name})", this);
            return null;
        }

        Renderer normal = AttachModel(socket.Model, bone, socket);
        Renderer broken = socket.BrokenModel != null ? AttachModel(socket.BrokenModel, bone, socket) : null;
        HitFeature.AddFlashRenderer(normal, index);
        HitFeature.AddFlashRenderer(broken, index);

        if (socket.HitRadius > 0f)
            AddHitShape(new HitShape(bone, socket.HitRadius, index));

        PartFeature part = AddFeature(new PartFeature(this, index, socket, bone, normal, broken));
        part.Broken += OnPartBroken;
        return part;
    }

    private static Renderer AttachModel(Renderer model, Transform bone, MonsterDef.MushroomSocket socket)
    {
        Renderer instance = Instantiate(model, bone);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(socket.Euler);
        instance.transform.localScale = Vector3.one * socket.Scale;
        return instance;
    }

    // 살아 있을 때 깬 버섯은 상급으로 확정 드랍. 휘청·다운 전환은 보스 상태(6번)에서 붙인다
    private void OnPartBroken(PartFeature part)
    {
        if (part.Socket.Drop == null)
            return;

        MushroomPickup pickup = ResourceManager.Instance.Spawn(part.Socket.Drop, part.Bone.position, Quaternion.identity);
        Vector2 spread = Random.insideUnitCircle * PartDropSpread;
        pickup.Launch(new Vector3(spread.x, PartDropUpSpeed, spread.y), IsDead ? EFreshness.Low : EFreshness.High);
    }

    // 부위 판정 구. 플레이 중이면 상태별 색(초록 멀쩡, 빨강 깨짐, 회색 장식)과 남은 내구도
    private void DrawPartGizmos()
    {
        IReadOnlyList<MonsterDef.MushroomSocket> listSocket = Def.ListMushroom;
        for (int i = 0; i < listSocket.Count; i++)
        {
            MonsterDef.MushroomSocket socket = listSocket[i];
            Transform bone = GetMushroomBone(socket.Part);
            if (bone == null || socket.HitRadius <= 0f)
                continue;

            PartFeature part = i < m_ListPart.Count ? m_ListPart[i] : null;
            Gizmos.color = GetPartGizmoColor(socket, part);
            Gizmos.DrawWireSphere(bone.position, socket.HitRadius);

#if UNITY_EDITOR
            string durability = part != null ? $"{part.Durability}/{socket.Durability}" : $"{socket.Durability}";
            UnityEditor.Handles.Label(bone.position + Vector3.up * socket.HitRadius, $"{i}:{socket.Part} {durability}");
#endif
        }
    }

    private static Color GetPartGizmoColor(MonsterDef.MushroomSocket socket, PartFeature part)
    {
        if (socket.IsBreakable == false)
            return Color.gray;

        return part != null && part.IsBroken ? Color.red : Color.green;
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

        m_Fsm.Change(EMonsterState.Hit);
    }

    protected override void OnDied()
    {
        m_Fsm.Change(EMonsterState.Dead);
    }
}
