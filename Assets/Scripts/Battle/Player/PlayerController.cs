using UnityEngine;
using UnityEngine.InputSystem;

public enum EPlayerState
{
    Move,
    Attack,
    Dash,
    Hit,
    Dead,
}

// 채집꾼 조작. 이동(WASD·방향키·왼스틱), 공격(클릭·스페이스·J·패드 A), 대시(Shift·패드 B).
// 공격 버튼을 누르면 가까운 몹 쪽으로 자동으로 돌아서 내려친다(모바일 대비)
[RequireComponent(typeof(MoveFeature), typeof(HitFeature), typeof(HealthFeature))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float m_MoveSpeed = 5f;
    [SerializeField, Tooltip("입력 세기가 이 값 이상이면 Run 애니")] private float m_RunThreshold = 0.6f;

    [Header("공격")]
    [SerializeField] private AttackSpec m_Attack = new("Attack", 1, 9, 1.1f, 1f, 0.7f, 7f, 0.07f, 2f);
    [SerializeField, Tooltip("이 거리 안의 몹 쪽으로 자동 회전")] private float m_AutoAimRange = 3.5f;
    [SerializeField, Tooltip("판정 뒤 이 시간이 지나면 다음 공격으로 끊을 수 있음")] private float m_CancelDelay = 0.12f;

    [Header("대시")]
    [SerializeField] private float m_DashSpeed = 16f;
    [SerializeField] private float m_DashTime = 0.18f;
    [SerializeField] private float m_DashCooldown = 0.5f;
    [SerializeField, Tooltip("대시 중 무적")] private bool m_IsDashInvincible = true;
    [SerializeField] private float m_DashAnimSpeed = 1.6f;

    [Header("피격")]
    [SerializeField] private float m_HitStunTime = 0.3f;

    private MoveFeature m_Move;
    private AnimFeature m_Anim;
    private HealthFeature m_Health;
    private HitFeature m_HitFeature;
    private MeleeAttackFeature m_Melee;
    private Transform m_CameraTransform;

    private InputAction m_MoveAction;
    private InputAction m_AttackAction;
    private InputAction m_DashAction;

    private EPlayerState m_State;
    private float m_TimeInState;
    private float m_AttackLength;
    private bool m_IsHitDone;
    private bool m_IsAttackQueued;
    private Vector3 m_DashDirection;
    private float m_DashCooldownRemain;

    public static PlayerController Current { get; private set; }

    public EPlayerState State => m_State;
    public bool IsDead => m_State == EPlayerState.Dead;
    public HealthFeature Health => m_Health;

    // ==================================================

    private void Awake()
    {
        Current = this;
        m_Move = GetComponent<MoveFeature>();
        m_Anim = GetComponent<AnimFeature>();
        m_Health = GetComponent<HealthFeature>();
        m_HitFeature = GetComponent<HitFeature>();
        m_Melee = GetComponent<MeleeAttackFeature>();

        InitInput();
    }

    private void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        m_MoveAction.Enable();
        m_AttackAction.Enable();
        m_DashAction.Enable();
        m_HitFeature.Hit += OnHit;
        m_Health.Died += OnDied;
    }

    private void OnDisable()
    {
        m_MoveAction.Disable();
        m_AttackAction.Disable();
        m_DashAction.Disable();
        m_HitFeature.Hit -= OnHit;
        m_Health.Died -= OnDied;
    }

    private void OnDestroy()
    {
        m_MoveAction.Dispose();
        m_AttackAction.Dispose();
        m_DashAction.Dispose();
        if (Current == this)
            Current = null;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        m_TimeInState += deltaTime;
        if (m_DashCooldownRemain > 0f)
            m_DashCooldownRemain -= deltaTime;

        bool isAttackPressed = m_AttackAction.WasPressedThisFrame();
        if (m_DashAction.WasPressedThisFrame() && CanDash())
        {
            ChangeState(EPlayerState.Dash);
            return;
        }

        switch (m_State)
        {
            case EPlayerState.Move:
                UpdateMove(isAttackPressed);
                break;
            case EPlayerState.Attack:
                UpdateAttack(isAttackPressed);
                break;
            case EPlayerState.Dash:
                UpdateDash();
                break;
            case EPlayerState.Hit:
                if (m_TimeInState >= m_HitStunTime)
                    ChangeState(EPlayerState.Move);
                break;
        }
    }

    public void Init()
    {
        if (Camera.main != null)
            m_CameraTransform = Camera.main.transform;
        if (m_Melee != null)
        {
            m_Melee.Init(ETeam.Player);
            m_Melee.SetGizmoSpec(m_Attack);
        }

        ChangeState(EPlayerState.Move);
    }

    // ==================================================

    private void InitInput()
    {
        m_MoveAction = new InputAction("Move", InputActionType.Value);
        m_MoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        m_MoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        // 모바일 조이스틱(OnScreenStick)도 이 경로로 들어온다
        m_MoveAction.AddBinding("<Gamepad>/leftStick");

        m_AttackAction = new InputAction("Attack", InputActionType.Button);
        m_AttackAction.AddBinding("<Mouse>/leftButton");
        m_AttackAction.AddBinding("<Keyboard>/space");
        m_AttackAction.AddBinding("<Keyboard>/j");
        m_AttackAction.AddBinding("<Gamepad>/buttonSouth");

        m_DashAction = new InputAction("Dash", InputActionType.Button);
        m_DashAction.AddBinding("<Keyboard>/leftShift");
        m_DashAction.AddBinding("<Keyboard>/rightShift");
        m_DashAction.AddBinding("<Gamepad>/buttonEast");
    }

    private void ChangeState(EPlayerState next)
    {
        m_Anim.SetSpeed(1f);
        m_State = next;
        m_TimeInState = 0f;

        switch (next)
        {
            case EPlayerState.Attack:
                EnterAttack();
                break;
            case EPlayerState.Dash:
                EnterDash();
                break;
            case EPlayerState.Hit:
                m_Move.Stop();
                m_Anim.Play("Hit", true);
                break;
            case EPlayerState.Dead:
                m_Move.Stop();
                m_Anim.Play("Death", true);
                break;
        }
    }

    private void UpdateMove(bool isAttackPressed)
    {
        if (isAttackPressed)
        {
            ChangeState(EPlayerState.Attack);
            return;
        }

        Vector3 input = GetMoveInput();
        float strength = Mathf.Clamp01(input.magnitude);
        if (strength < 0.1f)
        {
            m_Move.Stop();
            m_Anim.Play("Idle");
            return;
        }

        m_Move.SetVelocity(input.normalized * (m_MoveSpeed * strength));
        m_Move.FaceTo(input);
        m_Anim.Play(strength >= m_RunThreshold ? "Run" : "Walk");
    }

    private void EnterAttack()
    {
        m_IsHitDone = false;
        m_IsAttackQueued = false;
        m_AttackLength = m_Anim.GetLength(m_Attack.Clip);

        m_Move.Stop();
        Vector3 aimDirection = FindAimDirection();
        m_Move.FaceTo(aimDirection, true);
        m_Anim.Play(m_Attack.Clip, true);

        if (m_Attack.Lunge > 0f)
            m_Move.AddImpulse(transform.forward * m_Attack.Lunge);
    }

    private void UpdateAttack(bool isAttackPressed)
    {
        if (m_IsHitDone == false && m_TimeInState >= m_Attack.HitTime)
        {
            m_IsHitDone = true;
            if (m_Melee != null)
                m_Melee.Perform(m_Attack);
        }

        if (isAttackPressed)
            m_IsAttackQueued = true;

        // 판정이 끝난 뒤 입력이 있으면 다음 공격으로 끊는다
        bool canCancel = m_IsHitDone && m_TimeInState >= m_Attack.HitTime + m_CancelDelay;
        if (canCancel && m_IsAttackQueued)
        {
            ChangeState(EPlayerState.Attack);
            return;
        }

        if (m_TimeInState >= m_AttackLength)
            ChangeState(EPlayerState.Move);
    }

    // 이동 중이나 공격 판정이 끝난 뒤에만 대시로 끊을 수 있다
    private bool CanDash()
    {
        if (m_DashCooldownRemain > 0f)
            return false;

        switch (m_State)
        {
            case EPlayerState.Move:
                return true;
            case EPlayerState.Attack:
                return m_IsHitDone;
            default:
                return false;
        }
    }

    // 입력 방향으로, 입력이 없으면 지금 앞으로
    private void EnterDash()
    {
        Vector3 input = GetMoveInput();
        m_DashDirection = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;
        m_DashCooldownRemain = m_DashCooldown + m_DashTime;

        m_Move.FaceTo(m_DashDirection, true);
        m_Move.SetVelocity(m_DashDirection * m_DashSpeed);
        m_Anim.SetSpeed(m_DashAnimSpeed);
        m_Anim.Play("Run", true);

        if (m_IsDashInvincible)
            m_HitFeature.SetInvincible(m_DashTime);
    }

    private void UpdateDash()
    {
        if (m_TimeInState < m_DashTime)
            return;

        m_Move.Stop();
        ChangeState(EPlayerState.Move);
    }

    // 카메라 기준 입력 방향(수평)
    private Vector3 GetMoveInput()
    {
        Vector2 raw = m_MoveAction.ReadValue<Vector2>();
        if (m_CameraTransform == null)
            return new Vector3(raw.x, 0f, raw.y);

        Vector3 forward = m_CameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = m_CameraTransform.right;
        right.y = 0f;
        right.Normalize();
        return forward * raw.y + right * raw.x;
    }

    // 가까운 몹이 있으면 그쪽, 없으면 입력 방향, 그것도 없으면 지금 앞
    private Vector3 FindAimDirection()
    {
        MobController nearest = null;
        float nearestDistance = m_AutoAimRange;
        foreach (MobController mob in MobController.ListActive)
        {
            if (mob.IsDead)
                continue;

            float distance = Vector3.Distance(mob.transform.position, transform.position);
            if (distance >= nearestDistance)
                continue;

            nearest = mob;
            nearestDistance = distance;
        }

        if (nearest != null)
            return nearest.transform.position - transform.position;

        Vector3 input = GetMoveInput();
        return input.sqrMagnitude > 0.01f ? input : transform.forward;
    }

    private void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        ChangeState(EPlayerState.Hit);
    }

    private void OnDied()
    {
        ChangeState(EPlayerState.Dead);
    }
}
