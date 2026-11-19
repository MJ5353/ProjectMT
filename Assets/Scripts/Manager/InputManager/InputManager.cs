// ==================================================

// 입력 에셋(InputSystem_Actions)에서 생성한 C# 클래스 인스턴스를 하나만 들고 있다. 판단(PlayerBrain)·UI는 여기서 맵을 꺼내 쓴다.
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneControllerBase가 Create).
// !!) 프로젝트 전역 액션(InputSystem.actions)과는 별개의 사본이다. UI 입력 모듈은 전역 액션을 그대로 쓴다

public class InputManager : MonoSingleton<InputManager>
{
    private InputSystem_Actions m_Actions;

    public InputSystem_Actions.PlayerActions Player => m_Actions.Player;

    // ------------------------------------------------

    private void OnDestroy() => Dispose();

    private void Dispose()
    {
        m_Actions?.Dispose();
        m_Actions = null;
    }

    // virtual ---

    protected override void OnInit()
    {
        m_Actions = new InputSystem_Actions();
        m_Actions.Player.Enable();
    }

    protected override void OnClear() => Dispose();
}
