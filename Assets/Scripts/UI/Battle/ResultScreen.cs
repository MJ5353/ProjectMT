using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 결과 화면. BattleEnded를 받으면 열리고, BattleManager에서 시간·처치 수·주운 버섯을 읽어 보여 준다.
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

    private readonly List<UIItemSlot> m_ListSlot = new();

    public static ResultScreen Instance { get; private set; }
    public bool IsOpen { get; private set; }

    // ==================================================

    private void Awake()
    {
        Instance = this;

        m_SlotTemplate.gameObject.SetActive(false);
        m_RetryButton.onClick.AddListener(OnClickRetry);
        m_LobbyButton.onClick.AddListener(OnClickLobby);

        SetVisible(false);
    }

    private void OnEnable()
    {
        SEventService.Subscribe<BattleEnded>(OnBattleEnded);
    }

    private void OnDisable()
    {
        SEventService.Unsubscribe<BattleEnded>(OnBattleEnded);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open()
    {
        BattleManager battleM = BattleManager.Instance;
        if (battleM == null || IsOpen)
            return;

        bool isWin = battleM.State == EBattleState.Won;
        m_Title.text = isWin ? "사냥 성공" : "쓰러졌다";
        m_Title.color = isWin ? UIStyle.Gold : UIStyle.Accent;
        m_Summary.text = $"시간 {FormatTime(battleM.ElapsedTime)}  ·  처치 {battleM.KillCount}";

        FillLoot(battleM.DicLoot);
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
        foreach (UIItemSlot slot in m_ListSlot)
        {
            Destroy(slot.gameObject);
        }

        m_ListSlot.Clear();

        GameManager gameM = GameManager.Instance;
        foreach (var pair in dicLoot)
        {
            UIItemSlot slot = Instantiate(m_SlotTemplate, m_LootRoot);
            DataDef def = null;
            if (gameM != null && gameM.Database != null)
                gameM.Database.TryGet(pair.Key, out def);

            if (def != null)
                slot.Set(def, pair.Value);
            else
                slot.Set(pair.Key, null, pair.Key, pair.Value, UIStyle.PanelLight);

            slot.SetBadge(null, Color.clear);
            m_ListSlot.Add(slot);
        }

        m_EmptyLabel.SetActive(m_ListSlot.Count == 0);
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
            m_Window.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, EaseOutBack(t));
            yield return null;
        }

        m_Group.alpha = 1f;
        m_Window.localScale = Vector3.one;

        // 주운 버섯을 하나씩 튀어나오게 보여 준다
        foreach (UIItemSlot slot in m_ListSlot)
        {
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
            target.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(time / duration));
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
        foreach (UIItemSlot slot in m_ListSlot)
        {
            slot.gameObject.SetActive(false);
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        m_RetryButton.interactable = isInteractable;
        m_LobbyButton.interactable = isInteractable;
    }

    private void OnClickRetry()
    {
        BattleManager battleM = BattleManager.Instance;
        if (battleM == null)
            return;

        // 씬 로드 중 두 번 눌리지 않게 막는다
        SetButtonsInteractable(false);
        battleM.Retry();
    }

    private void OnClickLobby()
    {
        BattleManager battleM = BattleManager.Instance;
        if (battleM == null)
            return;

        SetButtonsInteractable(false);
        if (battleM.TryGoLobby() == false)
            SetButtonsInteractable(true);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
