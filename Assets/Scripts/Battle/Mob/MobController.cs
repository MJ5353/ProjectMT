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

// 몹 하나의 두뇌. 부품(이동·애니·체력·피격·공격)을 묶고 FSM으로 움직인다.
// 상태 클래스는 MobStates.cs, 수치는 MobDef
[RequireComponent(typeof(MoveFeature), typeof(HitFeature), typeof(HealthFeature))]
public class MobController : MonoBehaviour
{
    private static readonly List<MobController> s_ListActive = new();

    [SerializeField] private MobDef m_Def = null;
    [SerializeField, Tooltip("움츠림 연출에 쓰는 모델 루트")] private Transform m_VisualRoot = null;

    private readonly StateMachine<EMobState> m_Fsm = new();

    public static IReadOnlyList<MobController> ListActive => s_ListActive;

    public MobDef Def => m_Def;
    public Transform VisualRoot => m_VisualRoot;
    public MoveFeature Move { get; private set; }
    public AnimFeature Anim { get; private set; }
    public HealthFeature Health { get; private set; }
    public HitFeature HitFeature { get; private set; }
    public MeleeAttackFeature Melee { get; private set; }
    public Transform Target { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastHitDirection { get; private set; }
    public float CooldownRemain { get; set; }
    public EMobState State => m_Fsm.Current;
    public float TimeInState => m_Fsm.TimeInState;
    public bool IsDead => Health.IsDead;

    // ==================================================

    private void Awake()
    {
        Move = GetComponent<MoveFeature>();
        Anim = GetComponent<AnimFeature>();
        Health = GetComponent<HealthFeature>();
        HitFeature = GetComponent<HitFeature>();
        Melee = GetComponent<MeleeAttackFeature>();
    }

    private void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        s_ListActive.Add(this);
        HitFeature.Hit += OnHit;
        Health.Died += OnDied;
    }

    private void OnDisable()
    {
        s_ListActive.Remove(this);
        HitFeature.Hit -= OnHit;
        Health.Died -= OnDied;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (CooldownRemain > 0f)
            CooldownRemain -= deltaTime;

        if (Target == null && PlayerController.Current != null)
            Target = PlayerController.Current.transform;

        m_Fsm.Update(deltaTime);
    }

    public void Init()
    {
        if (m_Def == null)
        {
            CLogger.Error("MobController", $"MobDef가 비어 있습니다: {name}", this);
            enabled = false;
            return;
        }

        HomePosition = transform.position;
        Health.Init(m_Def.MaxHp);

        KnockbackFeature knockback = GetComponent<KnockbackFeature>();
        if (knockback != null)
            knockback.Init(m_Def.KnockbackScale);

        if (Melee != null)
        {
            Melee.Init(ETeam.Mob);
            Melee.SetGizmoSpec(m_Def.Attack);
        }

        AttachMushrooms();

        FlashFeature flash = GetComponent<FlashFeature>();
        if (flash != null)
            flash.Refresh();

        m_Fsm.Clear();
        m_Fsm.Add(EMobState.Idle, new MobIdleState(this));
        m_Fsm.Add(EMobState.Wander, new MobWanderState(this));
        m_Fsm.Add(EMobState.Chase, new MobChaseState(this));
        m_Fsm.Add(EMobState.Windup, new MobWindupState(this));
        m_Fsm.Add(EMobState.Attack, new MobAttackState(this));
        m_Fsm.Add(EMobState.Hit, new MobHitState(this));
        m_Fsm.Add(EMobState.Flee, new MobFleeState(this));
        m_Fsm.Add(EMobState.Dead, new MobDeadState(this));
        m_Fsm.Change(EMobState.Idle);
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

        return GetTargetDistance() <= m_Def.SightRange;
    }

    public void MoveTo(Vector3 direction, float speed)
    {
        direction.y = 0f;
        Move.SetVelocity(direction.normalized * speed);
        Move.FaceTo(direction);
    }

    // ==================================================

    private void AttachMushrooms()
    {
        foreach (MobDef.MushroomSocket socket in m_Def.ListMushroom)
        {
            if (socket.Prefab == null)
                continue;

            Transform bone = FindDeep(transform, socket.SocketName);
            if (bone == null)
            {
                CLogger.Warning("MobController", $"소켓을 찾지 못함: {socket.SocketName} ({name})", this);
                continue;
            }

            GameObject mushroom = Instantiate(socket.Prefab, bone);
            mushroom.transform.localPosition = Vector3.zero;
            mushroom.transform.localRotation = Quaternion.Euler(socket.Euler);
            mushroom.transform.localScale = Vector3.one * socket.Scale;
        }
    }

    private static Transform FindDeep(Transform parent, string targetName)
    {
        if (parent.name == targetName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        LastHitDirection = hit.Direction;
        if (hit.Attacker != null)
            Target = hit.Attacker.transform;

        m_Fsm.Change(EMobState.Hit);
    }

    private void OnDied()
    {
        m_Fsm.Change(EMobState.Dead);
    }
}
