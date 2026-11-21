using TMPro;
using UnityEngine;

// ==================================================

// 가로 게이지(체력, 재배 진행 등). 채움은 앵커 비율로 늘려서 스프라이트 없이 동작한다.
// 잔상(m_Trail)이 있으면 줄어든 만큼 잠깐 남았다가 따라 내려오고, 줄어드는 순간 안쪽(채움·잔상)이 잠깐 흔들린다.
// 몬스터 머리 위 바처럼 풀로 돌려 쓸 수도 있어 PoolableMonoUI를 상속한다(씬에 둔 게이지는 그냥 쓴다)

public class UIGauge : PoolableMonoUI
{
    [Header("Caching")]
    [SerializeField] private RectTransform m_Fill = null;
    [SerializeField] private RectTransform m_Trail = null;
    [SerializeField] private TMP_Text m_Text = null;
    
    [Header("Setting")]
    [SerializeField] private float m_TrailDelay = 0.4f;
    [SerializeField] private float m_TrailSpeed = 1.5f;
    [SerializeField] private float m_HitShake = 6f;        // 줄어들 때 흔들리는 폭(px). 0이면 안 흔들림
    [SerializeField] private float m_HitShakeTime = 0.2f;  // 흔들리는 시간(초)

    private float m_Ratio = 1f;
    private float m_TrailRatio = 1f;
    private float m_TrailWait = 0f;
    private float m_ShakeRemain = 0f;
    private Vector3 m_InnerBase;
    private bool m_HasInnerBase;

    public float Ratio => m_Ratio;

    // ------------------------------------------------

    private void Update()
    {
        // !!) 히트스톱이 timeScale을 건드려도 잔상은 계속 움직이게 unscaled를 쓴다
        UpdateTrail(Time.unscaledDeltaTime);
        UpdateShake(Time.unscaledDeltaTime);
    }

    public void SetValue(float current, float max, bool isImmediate = false)
    {
        float ratio = max > 0f ? current / max : 0f;
        SetRatio(ratio, isImmediate);
        
        m_Text.E_SetText($"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}");
    }

    public void SetRatio(float ratio, bool isImmediate = false)
    {
        ratio = Mathf.Clamp01(ratio);

        // 줄어들 때만 잔상을 남긴다
        if (isImmediate || ratio >= m_TrailRatio)
        {
            m_TrailRatio = ratio;
        }
        else if (ratio < m_Ratio)
        {
            m_TrailWait = m_TrailDelay;
            m_ShakeRemain = m_HitShakeTime;
        }

        m_Ratio = ratio;
        Apply(m_Fill, m_Ratio);
        Apply(m_Trail, m_TrailRatio);
    }

    public void SetText(string text)
    {
        m_Text.E_SetText(text);
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

    // 채움의 부모(안쪽)를 흔든다. 게이지 자체는 레이아웃이 잡고 있을 수 있어 건드리지 않는다
    private void UpdateShake(float deltaTime)
    {
        if (m_ShakeRemain <= 0f || m_Fill == null || m_HitShake <= 0f)
            return;

        Transform inner = m_Fill.parent;
        if (m_HasInnerBase == false)
        {
            m_InnerBase = inner.localPosition;
            m_HasInnerBase = true;
        }

        m_ShakeRemain -= deltaTime;
        
        float power = m_HitShakeTime > 0f ? Mathf.Clamp01(m_ShakeRemain / m_HitShakeTime) : 0f;
        inner.localPosition = m_InnerBase + (Vector3)(Random.insideUnitCircle * (m_HitShake * power));
    }

    // 흔들리던 중에 풀로 돌아갔을 수 있어 안쪽 위치를 되돌린다
    private void StopShake()
    {
        m_ShakeRemain = 0f;

        if (m_HasInnerBase == false || m_Fill.E_Alive() == false)
            return;
        
        m_Fill.parent.localPosition = m_InnerBase;
    }

    private static void Apply(RectTransform rt, float ratio)
    {
        if (rt == null)
            return;

        rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
    }

    // virtual ---

    public override void OnPoolSpawn()
    {
        base.OnPoolSpawn();
        
        StopShake();
        m_TrailWait = 0f;
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
