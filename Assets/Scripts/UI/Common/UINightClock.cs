using System.Collections;
using TMPro;
using UnityEngine;

// ==================================================

// 밤 카운트다운 시계. 둥근 마스크 안에서 하늘 띠(해 → 노을 → 밤 → 동틀녘)가 밤이 지난 만큼 내려간다. 전투·시장 공용.
// 전투: 남은 실제 시간을 mm:ss로 쓴다.
// 시장: 같은 자리 글자가 뒤집히며 인게임 시간("시장이 닫기까지, 4h 30m")으로 바뀌고, 아래에 "행동당 30분"이 뜬다.
// 환산은 Define.MarketActionTime(초) = Define.MarketActionMinutes(분)

public class UINightClock : MonoBehaviour
{
    private const int SkyPixelWidth = 48;   // HUD_NightClock_Sky 도트 폭
    private const int SkyScrollPixel = 192; // 하늘 띠 높이(240) - 보이는 창(48). 밤이 끝나면 이만큼 내려가 있다

    private const float ConvertHoldTime = 0.6f; // 시장이 열린 뒤 mm:ss를 보여 주는 시간
    private const float ConvertFlipTime = 0.18f; // 글자가 접혔다 펴지는 시간(각각)
    private const float SubFadeTime = 0.3f;

    [SerializeField] private RectTransform m_Sky = null; // 마스크(HUD_NightClock_Fill) 아래, 앵커·피벗 아래 가운데
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField] private TMP_Text m_SubText = null;  // 시장에서만 쓰는 작은 글자("행동당 30분"). 없어도 된다

    private int m_LastSeconds = -1;
    private int m_LastMinutes = -1;
    private bool m_IsMarket;
    private bool m_IsConverting;
    private float m_Remain;
    private TextAlignmentOptions m_BattleAlign;
    private Coroutine m_Convert;

    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    // ------------------------------------------------

    private void Awake()
    {
        m_BattleAlign = m_Text.alignment;
        if (m_SubText != null)
            m_SubText.gameObject.SetActive(false);
    }

    public void SetTime(float remain, float total)
    {
        m_Remain = Mathf.Max(0f, remain);
        float passed = total > 0f ? 1f - Mathf.Clamp01(remain / total) : 1f;

        // 도트가 번지지 않게 원본 1픽셀 단위로 끊어 내린다
        int pixel = Mathf.RoundToInt(SkyScrollPixel * passed);
        m_Sky.anchoredPosition = new Vector2(0f, -pixel * (m_Sky.rect.width / SkyPixelWidth));

        if (m_IsConverting)
            return;

        if (m_IsMarket)
            WriteMarketTime();
        else
            WriteRealTime();
    }

    // 시장 표기로 바꾼다. isAnimate면 mm:ss를 잠깐 보여 준 뒤 글자가 뒤집히며 바뀐다
    public void SetMarket(bool isMarket, bool isAnimate)
    {
        if (m_Convert != null)
        {
            StopCoroutine(m_Convert);
            m_Convert = null;
        }

        m_IsMarket = isMarket;
        m_IsConverting = false;
        m_LastSeconds = m_LastMinutes = -1;
        m_Text.rectTransform.localScale = Vector3.one;

        if (isMarket && isAnimate && gameObject.activeInHierarchy)
        {
            m_Convert = StartCoroutine(CoConvert());
            return;
        }

        ApplyMode();
    }

    // ---

    private void WriteRealTime()
    {
        // 글자는 초가 바뀔 때만 다시 쓴다
        int seconds = Mathf.CeilToInt(m_Remain);
        if (seconds == m_LastSeconds)
            return;

        m_LastSeconds = seconds;
        m_Text.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void WriteMarketTime()
    {
        int minutes = ToGameMinutes(m_Remain);
        if (minutes == m_LastMinutes)
            return;

        m_LastMinutes = minutes;
        m_Text.text = $"시장이 닫기까지, {minutes / 60}h {minutes % 60:00}m";
    }

    // 실제 초 → 인게임 분. 기본값이면 15초 = 30분(300초 = 10시간)
    private static int ToGameMinutes(float seconds)
    {
        float actionTime = Mathf.Max(0.01f, Define.MarketActionTime);
        return Mathf.FloorToInt(seconds / actionTime * Define.MarketActionMinutes + 0.001f);
    }

    private void ApplyMode()
    {
        // 시장 글자는 길어서 시계 왼쪽 끝부터 오른쪽으로 쓴다
        m_Text.alignment = m_IsMarket ? TextAlignmentOptions.Left : m_BattleAlign;

        if (m_SubText != null)
        {
            m_SubText.gameObject.SetActive(m_IsMarket);
            m_SubText.text = $"행동당 {Define.MarketActionMinutes}분";
            m_SubText.alpha = 1f;
        }

        if (m_IsMarket)
            WriteMarketTime();
        else
            WriteRealTime();
    }

    // mm:ss 잠깐 → 위아래로 접힘 → 인게임 시간으로 펴짐 → "행동당 30분" 서서히
    private IEnumerator CoConvert()
    {
        m_IsConverting = true;
        m_IsMarket = false;
        
        ApplyMode();
        m_IsMarket = true;

        // !!) 시장 중에는 timeScale과 무관하게 움직이도록 unscaled를 쓴다
        yield return new WaitForSecondsRealtime(ConvertHoldTime);

        RectTransform rt = m_Text.rectTransform;
        for (float t = 0f; t < ConvertFlipTime; t += Time.unscaledDeltaTime)
        {
            rt.localScale = new Vector3(1f, 1f - t / ConvertFlipTime, 1f);
            yield return null;
        }

        m_IsConverting = false;
        ApplyMode();

        if (m_SubText != null)
            m_SubText.alpha = 0f;

        for (float t = 0f; t < ConvertFlipTime; t += Time.unscaledDeltaTime)
        {
            rt.localScale = new Vector3(1f, t / ConvertFlipTime, 1f);
            yield return null;
        }

        rt.localScale = Vector3.one;

        if (m_SubText != null)
        {
            for (float t = 0f; t < SubFadeTime; t += Time.unscaledDeltaTime)
            {
                m_SubText.alpha = t / SubFadeTime;
                yield return null;
            }

            m_SubText.alpha = 1f;
        }

        m_Convert = null;
    }
}
