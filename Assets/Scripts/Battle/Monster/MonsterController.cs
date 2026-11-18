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
// 상태 클래스는 MonsterStates.cs, 수치는 MonsterDef
public class MonsterController : UnitController<MonsterDef>
{
    private readonly StateMachine<EMonsterState> m_Fsm = new();

    public Transform Target { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastHitDirection { get; private set; }
    public float CooldownRemain { get; set; }
    public EMonsterState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;

    protected override ETeam Team => ETeam.Monster;

    // ------------------------------------------------

    protected override void Set()
    {
        base.Set();
        if (enabled == false)
            return;

        AttachMushrooms();

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
        m_Fsm.Change(EMonsterState.Idle);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (CooldownRemain > 0f)
            CooldownRemain -= deltaTime;

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

        Vector3 diff = Target.position - transform.position;
        diff.y = 0f;
        return diff.magnitude;
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

    public void MoveTo(Vector3 direction, float speed)
    {
        direction.y = 0f;
        Move.SetVelocity(direction.normalized * speed);
        Move.FaceTo(direction);
    }

    // ==================================================

    // 버섯을 소켓 뼈에 꽂고, 맞을 때 같이 번쩍이도록 렌더러를 넘긴다
    private void AttachMushrooms()
    {
        foreach (MonsterDef.MushroomSocket socket in Def.ListMushroom)
        {
            if (socket.Model == null)
                continue;

            Transform bone = transform.E_FindDeep(socket.SocketName);
            if (bone == null)
            {
                CLogger.Warning("MonsterController", $"소켓을 찾지 못함: {socket.SocketName} ({name})", this);
                continue;
            }

            Renderer mushroom = Instantiate(socket.Model, bone);
            mushroom.transform.localPosition = Vector3.zero;
            mushroom.transform.localRotation = Quaternion.Euler(socket.Euler);
            mushroom.transform.localScale = Vector3.one * socket.Scale;
            Flash.AddRenderer(mushroom);
        }
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
