using TMPro;
using UnityEngine;

// 가로 게이지(체력, 재배 진행 등). 채움은 앵커 비율로 늘려서 스프라이트 없이 동작한다.
// 잔상(m_Trail)이 있으면 줄어든 만큼 잠깐 남았다가 따라 내려온다
public class UIGauge : MonoBehaviour
{
    [SerializeField] private RectTransform m_Fill = null;
    [SerializeField] private RectTransform m_Trail = null;
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField] private float m_TrailDelay = 0.4f;
    [SerializeField] private float m_TrailSpeed = 1.5f;

    private float m_Ratio = 1f;
    private float m_TrailRatio = 1f;
    private float m_TrailWait = 0f;

    public float Ratio => m_Ratio;

    // ==================================================

    private void Update()
    {
        // !!) 히트스톱이 timeScale을 건드려도 잔상은 계속 움직이게 unscaled를 쓴다
        UpdateTrail(Time.unscaledDeltaTime);
    }

    public void SetValue(float current, float max, bool isImmediate = false)
    {
        float ratio = max > 0f ? current / max : 0f;
        SetRatio(ratio, isImmediate);

        if (m_Text == null)
            return;

        m_Text.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void SetRatio(float ratio, bool isImmediate = false)
    {
        ratio = Mathf.Clamp01(ratio);

        // 줄어들 때만 잔상을 남긴다
        if (isImmediate || ratio >= m_TrailRatio)
            m_TrailRatio = ratio;
        else if (ratio < m_Ratio)
            m_TrailWait = m_TrailDelay;

        m_Ratio = ratio;
        Apply(m_Fill, m_Ratio);
        Apply(m_Trail, m_TrailRatio);
    }

    public void SetText(string text)
    {
        if (m_Text == null)
            return;

        m_Text.text = text;
    }

    private void UpdateTrail(float deltaTime)
    {
        if (m_Trail == null || m_TrailRatio <= m_Ratio)
            return;

        if (m_TrailWait > 0f)
        {
            m_TrailWait -= deltaTime;
            return;
        }

        m_TrailRatio = Mathf.MoveTowards(m_TrailRatio, m_Ratio, m_TrailSpeed * deltaTime);
        Apply(m_Trail, m_TrailRatio);
    }

    private static void Apply(RectTransform rt, float ratio)
    {
        if (rt == null)
            return;

        rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
    }

#if UNITY_EDITOR
    public void EditorSetup(RectTransform fill, RectTransform trail, TMP_Text text)
    {
        m_Fill = fill;
        m_Trail = trail;
        m_Text = text;
    }
#endif
}
