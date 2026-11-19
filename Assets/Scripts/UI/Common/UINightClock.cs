using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 밤 카운트다운 시계. 채움(Radial Fill)이 남은 밤만큼 줄고, 색이 하늘 → 노을 → 어두운 하늘로 바뀌며, 남은 시간을 mm:ss로 쓴다. 전투·시장 공용

public class UINightClock : MonoBehaviour
{
    [SerializeField] private Image m_Fill = null;    // Image.Type.Filled + Radial360. 흰 원 스프라이트를 색으로 물들인다
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField] private Gradient m_SkyColor = CreateSkyColor(); // 밤이 지난 비율(왼쪽 0 = 막 시작)에 따른 채움 색. 인스펙터에서 손본다

    private int m_LastSeconds = -1;

    // ------------------------------------------------

    public void SetTime(float remain, float total)
    {
        float ratio = total > 0f ? Mathf.Clamp01(remain / total) : 0f;
        m_Fill.fillAmount = ratio;
        m_Fill.color = m_SkyColor.Evaluate(1f - ratio);

        // 글자는 초가 바뀔 때만 다시 쓴다
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remain));
        if (seconds == m_LastSeconds)
            return;

        m_LastSeconds = seconds;
        m_Text.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    // 하늘색 → 노을색 → 어두운 하늘색
    private static Gradient CreateSkyColor()
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.82f, 0.96f), 0f),
                new GradientColorKey(new Color(0.98f, 0.58f, 0.35f), 0.5f),
                new GradientColorKey(new Color(0.16f, 0.2f, 0.42f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }
}
