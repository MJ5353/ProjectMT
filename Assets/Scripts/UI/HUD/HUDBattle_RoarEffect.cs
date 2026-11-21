using TMPro;
using UnityEngine;

// ==================================================

// 보스 포효 연출(HUD). BossRoared를 받으면 화면 가장자리가 붉게 번졌다 빠지고, 가운데 띠에 보스 이름이 잠깐 뜬다.
// 시간은 Define.RoarEffectTime. 히트스톱·일시정지와 상관없이 실제 시간으로 돈다

public class HUDBattle_RoarEffect : MonoBehaviour
{
    private const float FadeRatio = 0.2f; // 전체 시간 중 나타나고 사라지는 구간 비율(앞뒤 각각)

    [SerializeField] private CanvasGroup m_Vignette = null; // 붉은 가장자리(꽉 찬 이미지)
    [SerializeField] private CanvasGroup m_Banner = null;   // 이름 띠
    [SerializeField] private TMP_Text m_BannerText = null;

    private float m_Elapsed = -1f; // 0보다 작으면 꺼져 있다

    // ------------------------------------------------

    private void Awake()
    {
        m_Vignette.alpha = 0f;
        m_Banner.alpha = 0f;
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<BossRoared>(OnBossRoared);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<BossRoared>(OnBossRoared);
    }

    private void Update()
    {
        if (m_Elapsed < 0f)
            return;

        float total = GameManager.Instance.Define.RoarEffectTime;
        m_Elapsed += Time.unscaledDeltaTime;
        if (m_Elapsed >= total)
        {
            m_Elapsed = -1f;
            SetAlpha(0f);
            return;
        }

        // 앞 20% 동안 나타나고, 끝 20% 동안 사라진다
        float t = m_Elapsed / total;
        float alpha = t < FadeRatio ? t / FadeRatio : t > 1f - FadeRatio ? (1f - t) / FadeRatio : 1f;
        SetAlpha(alpha);
    }

    // ---

    private void OnBossRoared(BossRoared evt)
    {
        if (evt.Unit == null)
            return;

        string displayName = evt.Unit.Def.DisplayName;
        m_BannerText.text = evt.IsEnraged ? $"{displayName} 분노!" : displayName;
        m_Elapsed = 0f;
    }

    private void SetAlpha(float alpha)
    {
        m_Vignette.alpha = alpha * GameManager.Instance.Define.RoarVignetteAlpha;
        m_Banner.alpha = alpha;
    }
}
