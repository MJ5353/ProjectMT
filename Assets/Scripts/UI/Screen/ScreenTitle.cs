using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 타이틀 화면. 가운데 타이틀 그림, 아래 "클릭하여 시작하기"가 한 번 튀고 쉬기를 되풀이한다.
// 화면 전체를 덮는 배경 버튼을 누르면 버섯이 튀고 로비로 간다. 프리팹은 TitleUISetup.Prefabs 표로 만든다

public class ScreenTitle : ScreenBase
{
    [SerializeField] private Button m_StartButton = null;
    [SerializeField] private RectTransform m_PressLabel = null;

    private const float HopHeight = 4f;   // 튀는 높이
    private const float HopTime = 1.5f;       // 한 번 튀어 올랐다 내려오는 시간(초)
    private const float HopRestTime = 1f; // 다음 튀기까지 쉬는 시간(초)

    private readonly UISequence m_BobSequence = new();
    private readonly UISequence m_StartSequence = new();
    private Vector2 m_PressLabelPos;

    // ------------------------------------------------

    private void Awake()
    {
        m_StartButton.E_SetOnClick(OnClickStart);

        m_PressLabelPos = m_PressLabel.anchoredPosition;
        m_BobSequence.Tween(HopTime, t => m_PressLabel.anchoredPosition = m_PressLabelPos + Vector2.up * (Mathf.Sin(t * Mathf.PI) * HopHeight)).Wait(HopRestTime);
    }

    private void OnEnable() => PlayBob();

    private void OnDisable() => m_BobSequence.Stop();

    // 끝나면 다시 돌린다. 꺼져 있으면 Play가 바로 끝나 무한히 다시 부르므로 막는다
    private void PlayBob()
    {
        if (isActiveAndEnabled)
            m_BobSequence.Play(this, PlayBob);
    }

    // 누를 때마다 버섯이 튀고, 첫 클릭의 버섯이 다 퍼진 뒤에 로비로 넘어간다(넘어가는 순간 로딩 화면이 덮는다)
    private void OnClickStart()
    {
        GameManager gameM = GameManager.Instance;
        gameM.EffectS.PlayClickBurst();

        if (m_StartSequence.IsPlaying || gameM.CanChange(EGameState.Lobby) == false)
            return;

        m_StartSequence.Clear().Wait(gameM.Define.ClickBurstTime).Call(() => GameManager.Instance.ChangeState(EGameState.Lobby)).Play(this);
    }

#if UNITY_EDITOR
    public void EditorSetup(Button startButton, RectTransform pressLabel)
    {
        m_StartButton = startButton;
        m_PressLabel = pressLabel;
    }
#endif
}
