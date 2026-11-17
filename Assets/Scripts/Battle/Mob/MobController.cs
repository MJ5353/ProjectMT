using System.Collections.Generic;
using UnityEngine;

public enum EMobState
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

// 몹 하나의 두뇌. 몸(UnitController)에 FSM과 몸에 꽂는 버섯을 더한다.
// 상태 클래스는 MobStates.cs, 수치는 MobDef
public class MobController : UnitController<MobDef>
{
    private static readonly List<MobController> s_ListActive = new();

    private readonly StateMachine<EMobState> m_Fsm = new();

    public static IReadOnlyList<MobController> ListActive => s_ListActive;

    public Transform Target { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastHitDirection { get; private set; }
    public float CooldownRemain { get; set; }
    public EMobState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;

    protected override ETeam Team => ETeam.Mob;

    // ==================================================

    protected override void Awake()
    {
        base.Awake();
        if (enabled == false)
            return;

        AttachMushrooms();

        m_Fsm.Add(EMobState.Idle, new MobIdleState(this));
        m_Fsm.Add(EMobState.Wander, new MobWanderState(this));
        m_Fsm.Add(EMobState.Chase, new MobChaseState(this));
        m_Fsm.Add(EMobState.Windup, new MobWindupState(this));
        m_Fsm.Add(EMobState.Attack, new MobAttackState(this));
        m_Fsm.Add(EMobState.Hit, new MobHitState(this));
        m_Fsm.Add(EMobState.Flee, new MobFleeState(this));
        m_Fsm.Add(EMobState.Dead, new MobDeadState(this));
    }

    private void Start()
    {
        HomePosition = transform.position;
        m_Fsm.Change(EMobState.Idle);
    }

    private void OnEnable()
    {
        s_ListActive.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        s_ListActive.Remove(this);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (CooldownRemain > 0f)
            CooldownRemain -= deltaTime;

        if (Target == null && PlayerController.Current != null)
            Target = PlayerController.Current.transform;

        m_Fsm.Update(deltaTime);
    }

    public void ChangeState(EMobState next)
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
        if (Target == null)
            return false;

        if (PlayerController.Current != null && PlayerController.Current.IsDead)
            return false;

        return GetTargetDistance() <= Def.SightRange;
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
        foreach (MobDef.MushroomSocket socket in Def.ListMushroom)
        {
            if (socket.Model == null)
                continue;

            Transform bone = transform.E_FindDeep(socket.SocketName);
            if (bone == null)
            {
                CLogger.Warning("MobController", $"소켓을 찾지 못함: {socket.SocketName} ({name})", this);
                continue;
            }

            Renderer mushroom = Instantiate(socket.Model, bone);
            mushroom.transform.localPosition = Vector3.zero;
            mushroom.transform.localRotation = Quaternion.Euler(socket.Euler);
            mushroom.transform.localScale = Vector3.one * socket.Scale;
            Flash.AddRenderer(mushroom);
        }
    }

    protected override void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        LastHitDirection = hit.Direction;
        if (hit.Attacker != null)
            Target = hit.Attacker.transform;

        m_Fsm.Change(EMobState.Hit);
    }

    protected override void OnDied()
    {
        m_Fsm.Change(EMobState.Dead);
    }
}
