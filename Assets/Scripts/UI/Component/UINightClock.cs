using System.Collections;
using TMPro;
using UnityEngine;

// ==================================================

// 밤 카운트다운 시계. 둥근 마스크 안에서 하늘 띠(해 → 노을 → 밤 → 동틀녘)가 밤이 지난 만큼 내려간다. 전투·시장 공용.
// 전투: 남은 실제 시간을 mm:ss로 쓴다.
// 시장: 같은 자리에 인게임 시간("시장이 닫기까지, 4h 30m"), 아래에 "행동당 30분". 시간이 줄면 글자가 잠깐 붉게 튄다.
// 환산은 Define.Market.ActionTime(초) = Define.Market.ActionMinutes(분). 시장이 열릴 때의 연출은 PopupMarket이 한다.
// !!) 시장 중에는 시간이 멈춰 있으므로(TimeManager) 연출은 unscaled 시간으로 돈다

public class UINightClock : MonoBehaviour
{
    [SerializeField] private RectTransform m_Sky = null; // 마스크(HUD_NightClock_Fill) 아래, 앵커·피벗 아래 가운데
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField] private TMP_Text m_SubText = null;  // 시장에서만 쓰는 작은 글자("행동당 30분"). 없어도 된다
    [SerializeField] private int m_MarketFontSize = UIStyle.FontNormal; // 시장 글자는 길어서 조금 작게 쓴다

    #region const
    private const int SkyPixelWidth = 48;   // HUD_NightClock_Sky 도트 폭
    private const int SkyScrollPixel = 192; // 하늘 띠 높이(240) - 보이는 창(48). 밤이 끝나면 이만큼 내려가 있다
    private const float SpendTime = 0.35f;  // 시간이 줄 때 붉게 튀는 시간
    private const float SpendScale = 1.15f;
    #endregion

    private int m_LastSeconds = -1;
    private int m_LastMinutes = -1;
    private bool m_IsMarket;
    private float m_Remain;
    private float m_BattleFontSize;
    
    private TextAlignmentOptions m_BattleAlign;
    private Color m_BaseColor;
    private Coroutine m_Routine;

    public RectTransform TextRect => m_Text.rectTransform; // 시장 연출이 남은 시간 글자를 여기서 출발시킨다
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    // ------------------------------------------------
    
    public static string FormatRealTime(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    private void Awake()
    {
        m_BattleAlign = m_Text.alignment;
        m_BattleFontSize = m_Text.fontSize;
        m_BaseColor = m_Text.color;
        m_SubText.E_SetActive(false);
    }

    public void SetTime(float remain, float total)
    {
        m_Remain = Mathf.Max(0f, remain);
        float passed = total > 0f ? 1f - Mathf.Clamp01(remain / total) : 1f;

        // 도트가 번지지 않게 원본 1픽셀 단위로 끊어 내린다
        int pixel = Mathf.RoundToInt(SkyScrollPixel * passed);
        m_Sky.anchoredPosition = new Vector2(0f, -pixel * (m_Sky.rect.width / SkyPixelWidth));

        if (m_IsMarket)
            WriteMarketTime();
        else
            WriteRealTime();
    }

    public void SetMarket(bool isMarket)
    {
        StopRoutine();

        m_IsMarket = isMarket;
        m_LastSeconds = m_LastMinutes = -1;

        // 시장 글자는 길어서 시계 왼쪽 끝부터 오른쪽으로 쓴다
        m_Text.alignment = isMarket ? TextAlignmentOptions.Left : m_BattleAlign;
        m_Text.fontSize = isMarket ? m_MarketFontSize : m_BattleFontSize;

        m_SubText.E_SetStringWithActive(EStringKey.Clock_ActionTimeFormat, isMarket, Define.Market.ActionMinutes);

        if (isMarket)
            WriteMarketTime();
        else
            WriteRealTime();
    }

    // 시장 연출 동안 글자만 감춘다(글자가 화면 가운데로 날아간 것처럼)
    public void SetTextVisible(bool isVisible)
    {
        m_Text.enabled = isVisible;
        m_SubText.E_SetEnabled(isVisible); // 없어도 된다
    }

    // 실제 초 → 인게임 분. 기본값이면 15초 = 30분(300초 = 10시간)
    public static int ToGameMinutes(float seconds)
    {
        float actionTime = Mathf.Max(0.01f, Define.Market.ActionTime);
        return Mathf.FloorToInt(seconds / actionTime * Define.Market.ActionMinutes + 0.001f);
    }

    // ---

    private void WriteRealTime()
    {
        // 글자는 초가 바뀔 때만 다시 쓴다
        int seconds = Mathf.CeilToInt(m_Remain);
        if (seconds == m_LastSeconds)
            return;

        m_LastSeconds = seconds;
        m_Text.E_SetText(FormatRealTime(seconds));
    }

    private void WriteMarketTime()
    {
        int minutes = ToGameMinutes(m_Remain);
        if (minutes == m_LastMinutes)
            return;

        bool isSpent = m_LastMinutes >= 0 && minutes < m_LastMinutes;
        m_LastMinutes = minutes;
        m_Text.E_SetString(EStringKey.Clock_MarketCloseFormat, minutes / 60, minutes % 60);

        if (isSpent == false || gameObject.activeInHierarchy == false)
            return;

        StopRoutine();
        m_Routine = StartCoroutine(CoSpend());
    }

    private void StopRoutine()
    {
        if (m_Routine != null)
            StopCoroutine(m_Routine);

        m_Routine = null;
        m_Text.rectTransform.localScale = Vector3.one;
        m_Text.color = m_BaseColor;
    }

    // 거래·기다리기로 시간이 줄었다
    private IEnumerator CoSpend()
    {
        RectTransform rt = m_Text.rectTransform;
        
        yield return UITween.CoPlay(SpendTime, t =>
        {
            rt.localScale = Vector3.one * Mathf.Lerp(SpendScale, 1f, t);
            m_Text.color = Color.Lerp(UIStyle.Accent, m_BaseColor, t);
        });

        m_Routine = null;
    }
}
