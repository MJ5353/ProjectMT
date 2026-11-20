using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 결과 화면. BattleEnded를 받으면 열리고, BattleService에서 끝난 방식·시간·받은 보상(DicReward)을 읽어 보여 준다.
// [mj] todo) 신선 등급은 전투 쪽 데이터(ListLootGrade 가안)가 확정되면 칸 뱃지로 붙인다
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_Group = null;
    [SerializeField] private RectTransform m_Window = null;
    [SerializeField] private TMP_Text m_Title = null;
    [SerializeField] private TMP_Text m_Summary = null;
    [SerializeField] private RectTransform m_LootRoot = null;
    [SerializeField] private UIItemSlot m_SlotTemplate = null;
    [SerializeField] private GameObject m_EmptyLabel = null;
    [SerializeField] private Button m_RetryButton = null;
    [SerializeField] private Button m_LobbyButton = null;
    [SerializeField] private float m_FadeTime = 0.25f;
    [SerializeField] private float m_SlotInterval = 0.08f;

    private UIListPool<UIItemSlot> m_SlotPool;
    private int m_SlotCount; // 이번 결과에 쓰는 칸 수

    public static ResultScreen Instance { get; private set; }
    public bool IsOpen { get; private set; }

    // ==================================================

    private void Awake()
    {
        Instance = this;

        m_SlotTemplate.gameObject.SetActive(false);
        m_SlotPool = new UIListPool<UIItemSlot>(m_SlotTemplate, m_LootRoot);
        m_RetryButton.onClick.AddListener(OnClickRetry);
        m_LobbyButton.onClick.AddListener(OnClickLobby);

        SetVisible(false);
    }

    private void OnEnable()
    {
        SEventManager.Subscribe<BattleEnded>(OnBattleEnded);
    }

    private void OnDisable()
    {
        SEventManager.Unsubscribe<BattleEnded>(OnBattleEnded);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open()
    {
        BattleService battleM = GameManager.Instance?.BattleS;
        if (battleM == null || IsOpen)
            return;

        m_Title.text = battleM.State switch
        {
            EBattleState.Exited => "토벌 종료",
            EBattleState.TimedOut => "날이 밝았다",
            _ => "빈손으로 귀가",
        };
        m_Title.color = battleM.State == EBattleState.Exited ? UIStyle.Gold : UIStyle.Accent;
        m_Summary.text = $"시간 {FormatTime(battleM.ElapsedTime)}{FormatTrust(battleM.State)}";

        FillLoot(battleM.DicReward);
        m_LobbyButton.gameObject.SetActive(Application.CanStreamedLevelBeLoaded(SceneNames.Lobby));
        SetButtonsInteractable(true);

        IsOpen = true;
        StartCoroutine(OpenRoutine());
    }

    // ==================================================

    private void OnBattleEnded(BattleEnded evt)
    {
        Open();
    }

    private void FillLoot(IReadOnlyDictionary<string, int> dicLoot)
    {
        m_SlotCount = 0;

        // 칸은 꺼 둔 채 준비하고, OpenRoutine이 하나씩 켠다
        GameManager gameM = GameManager.Instance;
        foreach (var pair in dicLoot)
        {
            UIItemSlot slot = m_SlotPool.Get(m_SlotCount, false);
            DataDef def = null;
            if (gameM != null && gameM.Database != null)
                gameM.Database.TryGet(pair.Key, out def);

            if (def != null)
                slot.Set(def, pair.Value);
            else
                slot.Set(pair.Key, null, pair.Key, pair.Value, UIStyle.PanelLight);

            slot.SetBadge(null, Color.clear);
            ++m_SlotCount;
        }

        m_SlotPool.HideFrom(m_SlotCount);
        m_EmptyLabel.SetActive(m_SlotCount == 0);
    }

    // !!) 전투가 끝날 때 timeScale이 멈춰 있을 수 있어 연출은 모두 unscaled 시간으로 돈다
    private IEnumerator OpenRoutine()
    {
        SetVisible(true);
        m_Group.alpha = 0f;

        float time = 0f;
        while (time < m_FadeTime)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / m_FadeTime);

            m_Group.alpha = t;
            m_Window.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, UITween.EaseOutBack(t));
            yield return null;
        }

        m_Group.alpha = 1f;
        m_Window.localScale = Vector3.one;

        // 주운 버섯을 하나씩 튀어나오게 보여 준다
        for (int i = 0; i < m_SlotCount; ++i)
        {
            UIItemSlot slot = m_SlotPool.List[i];
            slot.gameObject.SetActive(true);
            StartCoroutine(PopRoutine(slot.transform));
            yield return new WaitForSecondsRealtime(m_SlotInterval);
        }
    }

    private static IEnumerator PopRoutine(Transform target)
    {
        const float duration = 0.18f;

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            target.localScale = Vector3.one * UITween.EaseOutBack(time / duration);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void SetVisible(bool isVisible)
    {
        m_Group.alpha = isVisible ? 1f : 0f;
        m_Group.interactable = isVisible;
        m_Group.blocksRaycasts = isVisible;

        if (isVisible)
            return;

        IsOpen = false;
        m_SlotPool.HideFrom(0);
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        m_RetryButton.interactable = isInteractable;
        m_LobbyButton.interactable = isInteractable;
    }

    private void OnClickRetry()
    {
        BattleService battleM = GameManager.Instance?.BattleS;
        if (battleM == null)
            return;

        // 씬 로드 중 두 번 눌리지 않게 막는다
        SetButtonsInteractable(false);
        battleM.Retry();
    }

    private void OnClickLobby()
    {
        BattleService battleM = GameManager.Instance?.BattleS;
        if (battleM == null)
            return;

        SetButtonsInteractable(false);
        if (battleM.TryGoLobby() == false)
            SetButtonsInteractable(true);
    }

    // 시장에서 오른 상인 신뢰도. "  ·  신뢰도 약재상 +10, 요리사 +5"
    private static string FormatTrust(EBattleState state)
    {
        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        GameDatabase database = GameManager.Instance?.Database;
        if (state != EBattleState.Exited || hunt == null || hunt.DicTrustGain.Count == 0)
            return string.Empty;

        System.Text.StringBuilder builder = new("  ·  신뢰도 ");
        bool isFirst = true;
        foreach (var pair in hunt.DicTrustGain)
        {
            MerchantDef merchant = null;
            database?.TryGet(pair.Key, out merchant);

            if (isFirst == false)
                builder.Append(", ");

            builder.Append($"{(merchant != null ? merchant.DisplayName : pair.Key)} +{pair.Value}");
            isFirst = false;
        }

        return builder.ToString();
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
