using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 맵에 들어오면 뜨는 시작 팝업(BattleFlow Start 상태가 Flow를 넘겨 띄운다). 화면 전체를 딤하고 맵 이름과 "클릭하여 시작"을 보여 준다.
// 어디를 눌러도 BattleFlow.BeginPlay를 부르고, 닫기는 Start 상태가 나갈 때 한다. 떠 있는 동안 유닛 판단은 멈춰 있다

public class PopupHuntStart : PopupFlowBase<BattleFlow>
{
    private const float BlinkTime = 1.2f;  // "클릭하여 시작" 깜빡임 한 번(초)
    private const float BlinkMinAlpha = 0.35f;

    [SerializeField] private Button m_Button = null;       // 화면 전체
    [SerializeField] private TMP_Text m_MapText = null;
    [SerializeField] private TMP_Text m_TapText = null;    // 깜빡인다

    public override bool IsCloseOnBack => false; // 눌러서 시작해야 한다

    // ------------------------------------------------

    private void Awake()
    {
        m_Button.E_SetOnClick(OnClickStart);
    }

    // 게임 시간과 무관하게 깜빡인다(로딩·히트스톱 영향 없음)
    private void Update()
    {
        float t = Mathf.PingPong(Time.unscaledTime * 2f / BlinkTime, 1f);
        m_TapText.alpha = Mathf.Lerp(BlinkMinAlpha, 1f, t);
    }

    // virtual ---

    protected override void OnOpen()
    {
        m_MapText.E_SetText(Flow.BattleS.Map.E_Alive()?.DisplayName);
    }

    // ---

    private void OnClickStart()
    {
        Flow.BeginPlay();
    }
}
