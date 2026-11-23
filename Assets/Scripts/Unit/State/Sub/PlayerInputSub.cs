using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ==================================================

// 플레이어 입력(유닛 수명 서브, 플레이어만). 입력을 읽어 상태(StateFeature)에 요청·예약만 한다. 이동은 기본 코어(FreeCore)에 넘긴다.
// 입력은 InputManager가 들고 있는 생성 클래스(InputSystem_Actions)의 Player 맵 Move·Attack·AttackUp·Dash를 쓴다.
// 키 배치는 에셋에서 바꾼다(기본: 이동 WASD·방향키·왼스틱, 공격 클릭·스페이스·J·패드 A/X, 대시 Shift·패드 B).
// 공격 버튼을 누르면 가까운 적 쪽으로 자동으로 돌아서 벤다(모바일 대비). 연타하면 Def.ComboStart부터 AttackSpec.Next 순서, 달리는 중이면 Def.RunAttack. 수치는 PlayerDef
// 퍼펙트 닷지가 오면 러시(RushSub)를 시작하고, 러시 중엔 클릭을 러시 공격으로 바꾼다. 히트스톱(deltaTime 0)·멈춤 중엔 StateFeature가 Tick하지 않는다

public class PlayerInputSub : SubState
{
    private const float AimInputMin = 0.01f;   // 이보다 작은 입력(제곱)은 방향 없음으로 본다

    private readonly PlayerDef m_Def;
    private readonly FreeCore m_Free;           // 기본 코어. 이동 입력을 넘긴다
    private readonly RushSub m_Rush;            // 퍼펙트 닷지 반격. 한 벌을 돌려 쓴다
    private readonly TargetSelector m_TargetSelector; // 가까운 적 조준
    private float m_AttackHoldTime;             // 공격 버튼을 누르고 있는 시간

    private static InputSystem_Actions.PlayerActions Actions => InputManager.Instance.Player;

    public override ESubLifetime Lifetime => ESubLifetime.Unit;

    // ------------------------------------------------

    public PlayerInputSub(UnitController owner, PlayerDef def, FreeCore free, RushSub rush) : base(owner)
    {
        m_Def = def;
        m_Free = free;
        m_Rush = rush;
        m_TargetSelector = new TargetSelector(owner);
    }

    protected override void Enter()
    {
        m_AttackHoldTime = 0f;
    }

    public override void Exit()
    {
        m_AttackHoldTime = 0f;
    }

    // 러시 중엔 다시 터질 일이 없다(러시 무적엔 판정 창이 없음)
    public override void OnPerfectDodge(in HitParams hit)
    {
        m_Rush.TryStart(hit.Attacker);
    }

    // 러시(Timed 서브)는 이미 이번 프레임 Tick을 마쳤다
    protected override void Tick(float deltaTime)
    {
        // 러시 중에는 대시·길게 누르기 없이 클릭만 받는다
        AttackSpec pressedAttack = m_Rush.IsActive ? GetRushAttack() : GetPressedAttack(deltaTime);
        if (m_Rush.IsActive == false && Actions.Dash.WasPressedThisFrame() && Owner.StateF.RequestDash(GetDashDirection()))
            return;

        UpdateAttackInput(pressedAttack);

        // 러시 돌진 중에는 러시가 이동을 정한다
        if (m_Rush.IsApproaching == false && Owner.StateF.IsIdle)
            m_Free.SetInput(GetMoveInput());
    }

    // ==================================================

    // 대기 중엔 바로, 공격 중엔 판정 시작 뒤 누른 것만 다음 타로 예약한다. 내는 건 서브 Tick 뒤 StateFeature가 한다
    private void UpdateAttackInput(AttackSpec pressedAttack)
    {
        StateFeature state = Owner.StateF;
        
        // 러시 막타 구간의 클릭은 베기 초반이라도 막타로 예약한다(한 번 누른 막타가 버려지지 않게)
        bool canBuffer = state.IsIdle || m_Rush.IsFinishWindow || (state.Current == ECoreState.Attack && state.Attack.CanBufferNext);
        if (pressedAttack != null && canBuffer)
        {
            state.ReserveAttack(pressedAttack, Vector3.zero);
            m_Rush.OnQueued(pressedAttack);
        }

        // 이번 프레임에 나갈 수 있을 때만 조준을 잰다(낼 때 한 번). 러시 돌진 중에는 예약만 해 두고 도착한 뒤 낸다
        AttackSpec reserved = state.ReservedAttack;
        if (reserved == null || m_Rush.IsApproaching || state.CanRequest(ECoreState.Attack) == false)
            return;

        state.ReserveAttack(reserved, FindAimDirection());
    }

    // get ---

    // 이번 프레임에 누른 공격. 위로 치기(1번)가 기본(0번)보다 먼저
    // 길게 누르면 누른 순간 나간 콤보 뒤에 HoldAttack이 이어진다
    private AttackSpec GetPressedAttack(float deltaTime)
    {
        if (IsHoldReached(deltaTime))
            return m_Def.HoldAttack.Attack;

        if (Actions.AttackUp.WasPressedThisFrame() && m_Def.UpAttack != null && IsClickOnUI(Actions.AttackUp) == false)
            return m_Def.UpAttack;

        if (Actions.Attack.WasPressedThisFrame() && IsClickOnUI(Actions.Attack) == false)
            return GetNextAttack();

        return null;
    }

    // 러시 중 이번 프레임 클릭(공격·위로 치기 어느 쪽이든). 길게 누르기는 재지 않는다
    private AttackSpec GetRushAttack()
    {
        m_AttackHoldTime = 0f;

        bool isPressed = (Actions.Attack.WasPressedThisFrame() && IsClickOnUI(Actions.Attack) == false)
                      || (Actions.AttackUp.WasPressedThisFrame() && IsClickOnUI(Actions.AttackUp) == false);

        return isPressed ? m_Rush.GetAttack() : null;
    }

    // 달리는 중이면 달리다 베기, 아니면 콤보 다음 타. 콤보가 없으면 기본 공격
    private AttackSpec GetNextAttack()
    {
        StateFeature state = Owner.StateF;
        if (state.IsIdle && m_Def.RunAttack != null && GetMoveInput().magnitude >= m_Def.RunThreshold)
            return m_Def.RunAttack.Attack;

        if (m_Def.ComboStart == null)
            return m_Def.MainAttack;

        // 공격이 끝나고 리셋 시간이 지나면 첫 타부터. 막타·콤보 밖 공격(Next 없음) 다음도 첫 타
        if (state.Current != ECoreState.Attack && state.TimeInAction > m_Def.ComboResetTime)
            return m_Def.ComboStart.Attack;

        return state.ComboNext ?? m_Def.ComboStart.Attack;
    }

    // 카메라 기준 입력 방향(수평)
    private Vector3 GetMoveInput()
    {
        Vector2 raw = Actions.Move.ReadValue<Vector2>();
        Transform cameraTransform = CameraManager.Instance.MainTransform;

        if (cameraTransform == null)
            return new Vector3(raw.x, 0f, raw.y);

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        return forward * raw.y + right * raw.x;
    }

    // 러시 중이면 퍼닷몹 쪽. 아니면 가까운 적이 있으면 그쪽, 없으면 입력 방향, 그것도 없으면 지금 앞
    private Vector3 FindAimDirection()
    {
        if (m_Rush.IsActive && m_Rush.TryGetAimDirection(out Vector3 rushDirection))
            return rushDirection;

        Vector3 input = GetMoveInput();
        Vector3 fallback = input.sqrMagnitude > AimInputMin ? input : Owner.transform.forward;
        return m_TargetSelector.GetAimDirection(m_Def.AutoAimRange, fallback);
    }

    // 입력 방향으로, 입력이 없으면 지금 앞으로
    private Vector3 GetDashDirection()
    {
        Vector3 input = GetMoveInput();
        return input.sqrMagnitude > AimInputMin ? input.normalized : Owner.transform.forward;
    }

    // 누른 시간이 HoldTime을 넘은 그 프레임만 true. 떼면 다시 잰다
    private bool IsHoldReached(float deltaTime)
    {
        if (m_Def.HoldAttack == null || Actions.Attack.IsPressed() == false)
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
