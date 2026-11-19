using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ==================================================

// 플레이어 판단 층. 입력을 읽어 행동 층(ActionFeature)에 요청만 한다.
// 입력은 프로젝트 입력 에셋(Assets/InputSystem_Actions)의 Player 맵 Move·Attack·AttackUp·Dash를 쓴다.
// 키 배치는 에셋에서 바꾼다(기본: 이동 WASD·방향키·왼스틱, 공격 클릭·스페이스·J·패드 A/X, 대시 Shift·패드 B).
// 공격 버튼을 누르면 가까운 적 쪽으로 자동으로 돌아서 벤다(모바일 대비). 연타하면 Def.ComboStart부터 AttackSpec.Next 순서, 달리는 중이면 Def.RunAttack. 수치는 PlayerDef

public class PlayerInputFeature : BrainFeature
{
    private const string PlayerMapName = "Player";
    private const string MoveActionName = "Player/Move";
    private const string AttackActionName = "Player/Attack";
    private const string AttackUpActionName = "Player/AttackUp";
    private const string DashActionName = "Player/Dash";

    private readonly PlayerDef m_Def;

    private Transform m_CameraTransform;

    private AttackSpec m_QueuedAttack; // 다음에 낼 공격. null이면 입력 없음
    private float m_AttackHoldTime;    // 공격 버튼을 누르고 있는 시간

    // input
    private InputAction m_MoveAction;
    private InputAction m_AttackAction;
    private InputAction m_AttackUpAction;
    private InputAction m_DashAction;

    // ------------------------------------------------

    public PlayerInputFeature(UnitController owner, PlayerDef def) : base(owner)
    {
        m_Def = def;
        InitInput();
    }

    public override void Reset()
    {
        base.Reset();
        m_QueuedAttack = null;
        m_AttackHoldTime = 0f;

        if (Camera.main != null)
            m_CameraTransform = Camera.main.transform;
    }

    // 히트스톱 중(deltaTime 0)에는 입력을 받지 않는다
    protected override void Think(float deltaTime)
    {
        if (deltaTime <= 0f || m_MoveAction == null)
            return;

        AttackSpec pressedAttack = GetPressedAttack(deltaTime);
        if (m_DashAction.WasPressedThisFrame() && Owner.ActionF.RequestDash(GetDashDirection()))
            return;

        UpdateAttackInput(pressedAttack);
        UpdateMoveInput();
    }

    // ==================================================

    // [mj] todo) 이쪽 대체?
    // 프로젝트 입력 에셋(Project Settings > Input System Package)의 액션을 찾는다. 여러 곳이 같이 쓰므로 끄거나 지우지 않는다
    private void InitInput()
    {
        InputActionAsset actions = InputSystem.actions;
        if (actions == null)
        {
            CLogger.Error("PlayerInputFeature", "프로젝트 입력 에셋이 지정되지 않았습니다. Project Settings > Input System Package에서 InputSystem_Actions를 지정하세요.", Owner);
            return;
        }

        m_MoveAction = actions.FindAction(MoveActionName, true);
        m_AttackAction = actions.FindAction(AttackActionName, true);
        m_AttackUpAction = actions.FindAction(AttackUpActionName, true);
        m_DashAction = actions.FindAction(DashActionName, true);

        actions.FindActionMap(PlayerMapName, true).Enable();
    }

    // 대기 중엔 바로, 공격 중엔 판정 시작 뒤 누른 것만 다음 타로 예약한다. 행동 층이 끊기를 허락해도 보낸다
    private void UpdateAttackInput(AttackSpec pressedAttack)
    {
        ActionFeature action = Owner.ActionF;
        bool canBuffer = action.IsIdle || (action.Current == EActionType.Attack && action.Attack.CanBufferNext);
        if (pressedAttack != null && canBuffer)
            m_QueuedAttack = pressedAttack;

        if (m_QueuedAttack == null || action.CanRequest(EActionType.Attack) == false)
            return;

        AttackSpec attack = m_QueuedAttack;
        m_QueuedAttack = null;
        action.RequestAttack(attack, FindAimDirection());
    }

    private void UpdateMoveInput()
    {
        Vector3 input = GetMoveInput();
        float strength = Mathf.Clamp01(input.magnitude);
        float speed = strength < 0.1f ? 0f : m_Def.MoveSpeed * strength;
        Owner.ActionF.SetMove(input, speed, strength >= m_Def.RunThreshold);
    }

    // get ---

    // 이번 프레임에 누른 공격. 위로 치기(1번)가 기본(0번)보다 먼저
    // 길게 누르면 누른 순간 나간 콤보 뒤에 HoldAttack이 이어진다
    private AttackSpec GetPressedAttack(float deltaTime)
    {
        if (IsHoldReached(deltaTime))
            return m_Def.HoldAttack.Attack;

        if (m_AttackUpAction.WasPressedThisFrame() && m_Def.UpAttack != null && IsClickOnUI(m_AttackUpAction) == false)
            return m_Def.UpAttack;

        if (m_AttackAction.WasPressedThisFrame() && IsClickOnUI(m_AttackAction) == false)
            return GetNextAttack();

        return null;
    }

    // 달리는 중이면 달리다 베기, 아니면 콤보 다음 타. 콤보가 없으면 기본 공격
    private AttackSpec GetNextAttack()
    {
        ActionFeature action = Owner.ActionF;
        if (action.IsIdle && m_Def.RunAttack != null && GetMoveInput().magnitude >= m_Def.RunThreshold)
            return m_Def.RunAttack.Attack;

        if (m_Def.ComboStart == null)
            return m_Def.MainAttack;

        // 공격이 끝나고 리셋 시간이 지나면 첫 타부터. 막타·콤보 밖 공격(Next 없음) 다음도 첫 타
        if (action.Current != EActionType.Attack && action.TimeInAction > m_Def.ComboResetTime)
            return m_Def.ComboStart.Attack;

        return action.ComboNext ?? m_Def.ComboStart.Attack;
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

    // 가까운 적이 있으면 그쪽, 없으면 입력 방향, 그것도 없으면 지금 앞
    private Vector3 FindAimDirection()
    {
        Vector3 input = GetMoveInput();
        Vector3 fallback = input.sqrMagnitude > 0.01f ? input : Owner.transform.forward;
        return Owner.TargetF.GetAimDirection(m_Def.AutoAimRange, fallback);
    }

    // 입력 방향으로, 입력이 없으면 지금 앞으로
    private Vector3 GetDashDirection()
    {
        Vector3 input = GetMoveInput();
        return input.sqrMagnitude > 0.01f ? input.normalized : Owner.transform.forward;
    }

    // 누른 시간이 HoldTime을 넘은 그 프레임만 true. 떼면 다시 잰다
    private bool IsHoldReached(float deltaTime)
    {
        if (m_Def.HoldAttack == null || m_AttackAction.IsPressed() == false)
        {
            m_AttackHoldTime = 0f;
            return false;
        }

        float before = m_AttackHoldTime;
        m_AttackHoldTime += deltaTime;
        return before < m_Def.HoldTime && m_AttackHoldTime >= m_Def.HoldTime;
    }

    // 결과 화면 버튼 같은 UI를 마우스로 누른 것이면 공격으로 치지 않는다
    private bool IsClickOnUI(InputAction action)
    {
        if (action.activeControl?.device is not Mouse)
            return false;

        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null && eventSystem.IsPointerOverGameObject();
    }
}
