using TMPro;
using UnityEngine;

// ==================================================

// 밤 카운트다운 시계. 둥근 마스크 안에서 하늘 띠(해 → 노을 → 밤 → 동틀녘)가 밤이 지난 만큼 내려가고, 남은 시간을 mm:ss로 쓴다. 전투·시장 공용

public class UINightClock : MonoBehaviour
{
    private const int SkyPixelWidth = 48;   // HUD_NightClock_Sky 도트 폭
    private const int SkyScrollPixel = 192; // 하늘 띠 높이(240) - 보이는 창(48). 밤이 끝나면 이만큼 내려가 있다

    [SerializeField] private RectTransform m_Sky = null; // 마스크(HUD_NightClock_Fill) 아래, 앵커·피벗 아래 가운데
    [SerializeField] private TMP_Text m_Text = null;

    private int m_LastSeconds = -1;

    // ------------------------------------------------

    public void SetTime(float remain, float total)
    {
        float passed = total > 0f ? 1f - Mathf.Clamp01(remain / total) : 1f;

        // 도트가 번지지 않게 원본 1픽셀 단위로 끊어 내린다
        int pixel = Mathf.RoundToInt(SkyScrollPixel * passed);
        m_Sky.anchoredPosition = new Vector2(0f, -pixel * (m_Sky.rect.width / SkyPixelWidth));

        // 글자는 초가 바뀔 때만 다시 쓴다
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remain));
        if (seconds == m_LastSeconds)
            return;

        m_LastSeconds = seconds;
        m_Text.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
