using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 퍼펙트 닷지 러시 연출(HUD). RushStarted를 받으면 화면을 어둡게 덮고 플레이어 주변만 동그랗게 밝히며, 빈틈 타이머 바와 "퍼펙트 닷지!" 문구를 띄운다.
// 어둠은 슬로우 동안만 깔린다(막타 정점에서 시간이 돌아오면 걷힌다). 바는 막타 구간을 따로 칠하고, 그 구간에 들어오면 "막타!"가 깜빡인다.
// 구멍 난 어둠 그림은 처음 한 번 코드로 만든다(텍스처 에셋 없음). 시간은 실제 시간(unscaled)

public class ScreenBattle_RushEffect : MonoBehaviour
{
    private const int HoleTextureSize = 512;
    private const float DimSize = 4096f;        // 어둠 이미지 한 변(캔버스 단위). 플레이어가 화면 끝에 있어도 화면을 다 덮는다
    private const float PlayerHeight = 1f;      // 밝힐 자리: 플레이어 발밑에서 이만큼 위
    private const float FinishBlinkSpeed = 10f; // 막타 구간 깜빡임 빠르기
    private const float BannerFadeRatio = 0.2f; // 문구가 나타나고 사라지는 구간 비율(앞뒤 각각)

    private static DefineDef Define => GameManager.Instance.Define;

    [SerializeField] private RectTransform m_Area = null;        // 어둠 위치 기준(화면 전체)
    [SerializeField] private Image m_Dim = null;                 // 가운데 구멍 난 어둠
    [SerializeField] private CanvasGroup m_Bar = null;           // 빈틈 타이머 바 묶음
    [SerializeField] private RectTransform m_BarFill = null;     // 남은 시간(가로 비율)
    [SerializeField] private Image m_BarFillImage = null;
    [SerializeField] private RectTransform m_FinishZone = null;  // 막타 구간(왼쪽부터 비율)
    [SerializeField] private CanvasGroup m_FinishLabel = null;   // "막타!"
    [SerializeField] private CanvasGroup m_Banner = null;        // "퍼펙트 닷지!"

    private PerfectDodgeRush m_Rush;   // 러시 중인 플레이어 것. 끝나면 null
    private Transform m_Player;
    private float m_TotalTime;
    private float m_DimAlpha;          // 어둠 진하기 0~1(Define.RushDimAlpha를 곱하기 전)
    private float m_BannerElapsed = -1f; // 0보다 작으면 꺼져 있다
    private Color m_FillColor;
    private Texture2D m_HoleTexture;
    private Sprite m_HoleSprite;

    // ------------------------------------------------

    private void Awake()
    {
        m_HoleTexture = CreateHoleTexture();
        m_HoleSprite = Sprite.Create(m_HoleTexture, new Rect(0, 0, HoleTextureSize, HoleTextureSize), new Vector2(0.5f, 0.5f));
        m_Dim.sprite = m_HoleSprite;
        m_Dim.rectTransform.sizeDelta = Vector2.one * DimSize;
        m_FillColor = m_BarFillImage.color;

        SetDimAlpha(0f);
        SetVisible(m_Bar, false);
        SetVisible(m_FinishLabel, false);
        SetVisible(m_Banner, false);
    }

    private void OnDestroy()
    {
        Destroy(m_HoleSprite);
        Destroy(m_HoleTexture);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<RushStarted>(OnRushStarted);
        SEventManager.Subscribe<RushEnded>(OnRushEnded);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<RushStarted>(OnRushStarted);
        SEventManager.Unsubscribe<RushEnded>(OnRushEnded);
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        UpdateDim(deltaTime);
        UpdateBar();
        UpdateBanner(deltaTime);
    }

    // ==================================================

    // 러시 중이고 슬로우가 걸려 있는 동안만 어둡게. 플레이어 화면 위치를 따라간다
    private void UpdateDim(float deltaTime)
    {
        bool isDark = m_Rush != null && TimeManager.Instance.IsSlow;
        m_DimAlpha = Mathf.MoveTowards(m_DimAlpha, isDark ? 1f : 0f, deltaTime / Define.RushDimFadeTime);
        SetDimAlpha(m_DimAlpha);

        if (m_DimAlpha <= 0f || m_Player == null)
            return;

        Camera mainCamera = CameraManager.Instance.Main;
        if (mainCamera == null)
            return;

        // 오버레이 캔버스라 카메라 없이 화면 좌표를 바꾼다
        Vector3 screen = mainCamera.WorldToScreenPoint(m_Player.position + Vector3.up * PlayerHeight);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Area, screen, null, out Vector2 local))
            m_Dim.rectTransform.anchoredPosition = local;
    }

    // 돌진 중엔 가득, 빈틈 타이머가 돌면 줄고, 막타를 내면 숨긴다. 막타 구간에선 채움이 금색으로 깜빡이고 "막타!"가 뜬다
    private void UpdateBar()
    {
        if (m_Rush == null)
            return;

        if (m_Rush.Phase == ERushPhase.Finisher)
        {
            SetVisible(m_Bar, false);
            SetVisible(m_FinishLabel, false);
            return;
        }

        float ratio = m_Rush.Phase == ERushPhase.Approach ? 1f : Mathf.Clamp01(m_Rush.Remain / m_TotalTime);
        m_BarFill.anchorMax = new Vector2(ratio, 1f);

        bool isFinish = m_Rush.IsFinishWindow;
        float blink = isFinish ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * FinishBlinkSpeed) : 0f;
        m_BarFillImage.color = isFinish ? Color.Lerp(m_FillColor, UIStyle.Gold, blink) : m_FillColor;

        SetVisible(m_FinishLabel, isFinish);
        if (isFinish)
            m_FinishLabel.alpha = 0.4f + 0.6f * blink;
    }

    // 앞 20% 동안 나타나고, 끝 20% 동안 사라진다
    private void UpdateBanner(float deltaTime)
    {
        if (m_BannerElapsed < 0f)
            return;

        float total = Define.RushBannerTime;
        m_BannerElapsed += deltaTime;

        if (m_BannerElapsed >= total)
        {
            m_BannerElapsed = -1f;
            SetVisible(m_Banner, false);
            return;
        }

        float t = m_BannerElapsed / total;
        float alpha = 1f;

        if (t < BannerFadeRatio)
            alpha = t / BannerFadeRatio;
        else if (t > 1f - BannerFadeRatio)
            alpha = (1f - t) / BannerFadeRatio;

        m_Banner.alpha = alpha;
    }

    // 클릭을 막지 않게 alpha만 바꾼다(E_SetVisible은 보일 때 blocksRaycasts를 켠다)
    private static void SetVisible(CanvasGroup group, bool isVisible)
    {
        group.alpha = isVisible ? 1f : 0f;
    }

    private void SetDimAlpha(float alpha)
    {
        Color color = m_Dim.color;
        color.a = alpha * Define.RushDimAlpha;
        m_Dim.color = color;
    }

    // 가운데가 투명하고 바깥으로 갈수록 어두워지는 원. 반지름·번짐은 캔버스 단위를 텍스처 비율로 옮긴다
    private static Texture2D CreateHoleTexture()
    {
        Texture2D texture = new(HoleTextureSize, HoleTextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float unitPerPixel = DimSize / HoleTextureSize;
        float radius = Define.RushLightRadius;
        float softness = Mathf.Max(1f, Define.RushLightSoftness);
        float center = (HoleTextureSize - 1) * 0.5f;

        Color32[] pixels = new Color32[HoleTextureSize * HoleTextureSize];
        for (int y = 0; y < HoleTextureSize; y++)
        {
            for (int x = 0; x < HoleTextureSize; x++)
            {
                float distance = new Vector2(x - center, y - center).magnitude * unitPerPixel;
                float alpha = Mathf.SmoothStep(0f, 1f, (distance - radius) / softness);
                pixels[y * HoleTextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // on event ---

    private void OnRushStarted(RushStarted evt)
    {
        if (evt.Unit.BrainBase is not PlayerBrain brain)
            return;

        m_Rush = brain.Rush;
        m_Player = evt.Unit.transform;
        m_TotalTime = Mathf.Max(evt.Time, 0.01f);
        m_FinishZone.anchorMax = new Vector2(Mathf.Clamp01(evt.FinishWindow / m_TotalTime), 1f);

        SetVisible(m_Bar, true);
        SetVisible(m_Banner, true);
        m_Banner.alpha = 0f;
        m_BannerElapsed = 0f;
    }

    // 어둠은 Update에서 천천히 걷힌다
    private void OnRushEnded(RushEnded evt)
    {
        m_Rush = null;
        SetVisible(m_Bar, false);
        SetVisible(m_FinishLabel, false);
    }
}
