using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum EPlayerState
{
    Move,
    Attack,
    Dash,
    Hit,
    Dead,
}

// 채집꾼 조작. 입력은 프로젝트 입력 에셋(Assets/InputSystem_Actions)의 Player 맵 Move·Attack·Dash를 쓴다.
// 키 배치는 에셋에서 바꾼다(기본: 이동 WASD·방향키·왼스틱, 공격 클릭·스페이스·J·패드 A/X, 대시 Shift·패드 B).
// 공격 버튼을 누르면 가까운 몬스터 쪽으로 자동으로 돌아서 내려친다(모바일 대비). 수치는 PlayerDef
public class PlayerController : UnitController<PlayerDef>
{
    private const string PlayerMapName = "Player";
    private const string MoveActionName = "Player/Move";
    private const string AttackActionName = "Player/Attack";
    private const string DashActionName = "Player/Dash";

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

    public EPlayerState State => m_State;

    protected override ETeam Team => ETeam.Player;

    // ------------------------------------------------

    protected override void Set()
    {
        InitInput();
        base.Set();
    }


    protected override void OnUpdate(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        m_TimeInState += deltaTime;
        if (m_DashCooldownRemain > 0f)
            m_DashCooldownRemain -= deltaTime;

        if (m_MoveAction == null)
            return;

        bool isAttackPressed = m_AttackAction.WasPressedThisFrame() && IsClickOnUI() == false;
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
                if (m_TimeInState >= Def.HitStunTime)
                    ChangeState(EPlayerState.Move);

                break;
        }
    }

    // 몬스터가 노리거나 버섯이 빨려 들 수 있는 상태인지. 판단 기준은 여기서만 정한다
    public bool IsValid()
    {
        return IsDead == false;
    }

    protected override void OnSpawned()
    {
        m_DashCooldownRemain = 0f;
        m_IsAttackQueued = false;
        Init();
    }

    public void Init()
    {
        if (Camera.main != null)
            m_CameraTransform = Camera.main.transform;

        ChangeState(EPlayerState.Move);
    }

    // ==================================================

    // 프로젝트 입력 에셋(Project Settings > Input System Package)의 액션을 찾는다. 여러 곳이 같이 쓰므로 끄거나 지우지 않는다
    private void InitInput()
    {
        InputActionAsset actions = InputSystem.actions;
        if (actions == null)
        {
            CLogger.Error("PlayerController", "프로젝트 입력 에셋이 지정되지 않았습니다. Project Settings > Input System Package에서 InputSystem_Actions를 지정하세요.", this);
            return;
        }

        m_MoveAction = actions.FindAction(MoveActionName, true);
        m_AttackAction = actions.FindAction(AttackActionName, true);
        m_DashAction = actions.FindAction(DashActionName, true);
        actions.FindActionMap(PlayerMapName, true).Enable();
    }

    // 결과 화면 버튼 같은 UI를 마우스로 누른 것이면 공격으로 치지 않는다
    private bool IsClickOnUI()
    {
        if (m_AttackAction.activeControl?.device is not Mouse)
            return false;

        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null && eventSystem.IsPointerOverGameObject();
    }

    private void ChangeState(EPlayerState next)
    {
        Anim.SetSpeed(1f);
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
                Move.Stop();
                Anim.Play("Hit", true);
                break;
            case EPlayerState.Dead:
                Move.Stop();
                Anim.Play("Death", true);
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
            Move.Stop();
            Anim.Play("Idle");
            return;
        }

        Move.SetVelocity(input.normalized * (Def.MoveSpeed * strength));
        Move.FaceTo(input);
        Anim.Play(strength >= Def.RunThreshold ? "Run" : "Walk");
    }

    private void EnterAttack()
    {
        m_IsHitDone = false;
        m_IsAttackQueued = false;
        m_AttackLength = Anim.GetLength(Def.Attack.Clip);

        Move.Stop();
        Vector3 aimDirection = FindAimDirection();
        Move.FaceTo(aimDirection, true);
        Anim.Play(Def.Attack.Clip, true);

        if (Def.Attack.Lunge > 0f)
            Move.AddImpulse(transform.forward * Def.Attack.Lunge);
    }

    private void UpdateAttack(bool isAttackPressed)
    {
        if (m_IsHitDone == false && m_TimeInState >= Def.Attack.HitTime)
        {
            m_IsHitDone = true;
            Melee.Perform(Def.Attack);
        }

        if (isAttackPressed)
            m_IsAttackQueued = true;

        // 판정이 끝난 뒤 입력이 있으면 다음 공격으로 끊는다
        bool canCancel = m_IsHitDone && m_TimeInState >= Def.Attack.HitTime + Def.CancelDelay;
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
        m_DashCooldownRemain = Def.DashCooldown + Def.DashTime;

        Move.FaceTo(m_DashDirection, true);
        Move.SetVelocity(m_DashDirection * Def.DashSpeed);
        Anim.SetSpeed(Def.DashAnimSpeed);
        Anim.Play("Run", true);

        if (Def.IsDashInvincible)
            HitFeature.SetInvincible(Def.DashTime);
    }

    private void UpdateDash()
    {
        if (m_TimeInState < Def.DashTime)
            return;

        Move.Stop();
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

    // 가까운 몬스터이 있으면 그쪽, 없으면 입력 방향, 그것도 없으면 지금 앞
    private Vector3 FindAimDirection()
    {
        IHitTarget nearest = GameManager.Instance?.Actors?.FindNearestEnemy(transform.position, Def.AutoAimRange, Team);
        if (nearest != null)
            return nearest.transform.position - transform.position;

        Vector3 input = GetMoveInput();
        return input.sqrMagnitude > 0.01f ? input : transform.forward;
    }

    protected override void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        ChangeState(EPlayerState.Hit);
    }

    protected override void OnDied()
    {
        ChangeState(EPlayerState.Dead);
    }
}
