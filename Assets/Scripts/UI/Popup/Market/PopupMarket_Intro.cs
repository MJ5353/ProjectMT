using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ==================================================

// 시장 시작 연출. 시계의 남은 시간이 화면 가운데로 날아와 주르륵 줄고, 그 아래 "지금 가면... n시간 n분은 거래할 수 있겠어!"가 후루룩 오른다.
// 단계는 PopupMarket이 여는 연출(OnBuildOpenSequence)에 Append로 붙이고, 끝 모습에서 Continue를 받을 때까지 기다린다.
// 누를 때 건너뛸지 넘길지는 여는 연출을 가진 PopupMarket이 정한다(SetOnClick)

public class PopupMarket_Intro : MonoBehaviour
{
    private const float MoveTime = 0.9f;   // 남은 시간이 가운데로 날아오는 시간
    private const float CountTime = 2.4f;  // 남은 시간이 줄고 시장 시간이 오르는 시간
    private const int BigSize = UIStyle.FontTitle;

    [SerializeField] private CanvasGroup m_Group = null;
    [SerializeField] private Button m_Button = null;         // 연출 전체를 덮는 투명 버튼
    [SerializeField] private TMP_Text m_TimerText = null;    // 가운데 큰 mm:ss
    [SerializeField] private TMP_Text m_MessageText = null;  // 그 아래 "지금 가면... n시간 n분은 거래할 수 있겠어!"

    private bool m_IsContinue;  // 끝 모습에서 눌렀다
    private Vector3 m_From;     // 시계 아래 남은 시간 자리
    private Vector3 m_To;       // 가운데 큰 mm:ss 자리
    private float m_FromScale;  // 시계 글자 크기에 맞춘 시작 크기
    private int m_Seconds;      // 남은 실제 초
    private int m_Minutes;      // 시장 시간(게임 분)

    // ------------------------------------------------

    public void SetOnClick(UnityAction onClick) => m_Button.E_SetOnClick(onClick);

    // 끝 모습에서 기다리던 것을 푼다
    public void Continue() => m_IsContinue = true;

    // 연출 시작 모습. 남은 시간 글자를 시계 글자(clockText) 자리·크기에 둔다
    public void Ready(float remain, RectTransform clockText)
    {
        m_Seconds = Mathf.CeilToInt(remain);
        m_Minutes = UINightClock.ToGameMinutes(remain);
        m_IsContinue = false;

        m_Group.E_SetActive(true);
        m_Group.alpha = 1f;

        RectTransform timer = m_TimerText.rectTransform;
        m_To = timer.position;
        m_From = clockText.position;
        m_FromScale = clockText.rect.height / Mathf.Max(1f, timer.rect.height);
        
        m_TimerText.E_SetText(UINightClock.FormatRealTime(m_Seconds));
        m_MessageText.E_SetText(FormatMessage(0));
        m_MessageText.alpha = 0f;
    }

    // 날아오기 → 숫자 굴리기 → 끝 모습 → Continue까지 기다리기
    public UISequence Append(UISequence sequence)
    {
        return sequence
            .Tween(MoveTime, TweenFly) // 1. 시계 아래 남은 시간이 가운데로 날아오며 커진다
            .Tween(CountTime, TweenCount) // 2. 남은 시간은 주르륵 줄고, 시장 시간은 후루룩 오른다
            .Call(ShowEnd) // 끝 모습에서 멈춰 있다가
            .WaitUntil(() => m_IsContinue); // 한 번 더 누르면 넘어간다
    }

    public void SetAlpha(float alpha) => m_Group.alpha = alpha;

    public void Hide() => m_Group.E_SetActive(false);

    // ---

    private void TweenFly(float t)
    {
        RectTransform timer = m_TimerText.rectTransform;

        float k = UITween.EaseOutCubic(t);
        timer.position = Vector3.Lerp(m_From, m_To, k);
        timer.localScale = Vector3.one * Mathf.Lerp(m_FromScale, 1f, k);
        
        m_MessageText.alpha = k;
    }

    private void TweenCount(float t)
    {
        float k = UITween.EaseOutCubic(t);
        m_TimerText.E_SetText(UINightClock.FormatRealTime(Mathf.RoundToInt(m_Seconds * (1f - k))));
        m_MessageText.E_SetText(FormatMessage(Mathf.RoundToInt(m_Minutes * k / 10f) * 10)); // 10분 단위로 굴러간다
    }

    private void ShowEnd()
    {
        m_TimerText.E_SetText(UINightClock.FormatRealTime(0));
        
        string strTag = UIStyle.ColorTag(UIStyle.TextDim, StringDef.S_Get(EStringKey.Market_IntroContinue));
        string strMessage = $"{FormatMessage(m_Minutes)}\n<size={UIStyle.FontSmall}>{strTag}</size>";
        m_MessageText.E_SetText(strMessage);
    }

    // "지금 가면... 4시간 30분은 거래할 수 있겠어!" (시간은 크게, 금색)
    private static string FormatMessage(int minutes)
    {
        int hour = minutes / 60;
        int minute = minutes % 60;
        
        string time = hour > 0 && minute == 0 ? StringDef.S_Format(EStringKey.Market_HourFormat, hour)
            : hour > 0 ? StringDef.S_Format(EStringKey.Market_HourMinuteFormat, hour, minute)
            : StringDef.S_Format(EStringKey.Market_MinuteFormat, minute);

        return StringDef.S_Format(EStringKey.Market_IntroFormat, $"<size={BigSize}>{UIStyle.ColorTag(UIStyle.Gold, time)}</size>");
    }
}
