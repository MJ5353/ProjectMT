using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 결과 팝업. BattleEnded를 받은 HUDBattle이 PopupService로 띄우고, BattleService에서 끝난 방식·시간·받은 보상(DicReward)을 읽어 보여 준다.
// [mj] todo) 신선 등급은 전투 쪽 데이터(ListLootGrade 가안)가 확정되면 칸 뱃지로 붙인다

public class PopupResult : Popup
{
    private const float SlotPopTime = 0.18f; // 칸 하나가 튀어나오는 시간

    [SerializeField] private TMP_Text m_Title = null;
    [SerializeField] private TMP_Text m_Summary = null;
    [SerializeField] private UIListLayout m_LootRoot = null;
    [SerializeField] private GameObject m_EmptyLabel = null;
    [SerializeField] private Button m_RetryButton = null;
    [SerializeField] private Button m_LobbyButton = null;
    
    [SerializeField] private float m_SlotInterval = 0.08f;

    private UIListView<UICommonSlot> m_SlotPool;
    private readonly List<KeyValuePair<string, int>> m_ListLoot = new(); // 이번 결과에 보여 줄 보상(ID, 개수). m_SlotPool 순서

    public override bool IsCloseOnBack => false; // 다시 하기/로비로 중 하나를 골라야 한다

    // ------------------------------------------------

    private void Awake()
    {
        m_SlotPool = new UIListView<UICommonSlot>(EUIComponentType.UICommonSlot, m_LootRoot);
        m_RetryButton.onClick.AddListener(OnClickRetry);
        m_LobbyButton.onClick.AddListener(OnClickLobby);
    }

    // virtual ---

    protected override void OnOpen()
    {
        BattleService battleS = GameManager.Instance?.BattleS;
        if (battleS == null)
            return;

        m_Title.text = battleS.State switch
        {
            EBattleState.Exited => "토벌 종료",
            EBattleState.TimedOut => "날이 밝았다",
            _ => "빈손으로 귀가",
        };
        m_Title.color = battleS.State == EBattleState.Exited ? UIStyle.Gold : UIStyle.Accent;
        m_Summary.text = $"시간 {UINightClock.FormatRealTime(Mathf.FloorToInt(battleS.ElapsedTime))}{FormatTrust(battleS.State)}";

        FillLoot(battleS.DicReward);
        m_LobbyButton.gameObject.SetActive(Application.CanStreamedLevelBeLoaded(SceneNames.Lobby));
        SetButtonsInteractable(true);
    }

    #region OnClick
    
    private void OnClickRetry()
    {
        BattleService battleS = GameManager.Instance?.BattleS;
        if (battleS == null)
            return;

        // 씬 로드 중 두 번 눌리지 않게 막는다
        SetButtonsInteractable(false);
        battleS.Retry();
    }

    private void OnClickLobby()
    {
        BattleService battleS = GameManager.Instance?.BattleS;
        if (battleS == null)
            return;

        SetButtonsInteractable(false);
        
        if (battleS.TryGoLobby() == false)
            SetButtonsInteractable(true);
    }

    #endregion
    
    // ---

    private void FillLoot(IReadOnlyDictionary<string, int> dicLoot)
    {
        m_ListLoot.Clear();
        foreach (var pair in dicLoot)
        {
            m_ListLoot.Add(pair);
        }

        // 칸은 꺼 둔 채 준비하고, CoAfterOpen이 하나씩 켠다(그래서 Refresh 대신 Get)
        for (int i = 0; i < m_ListLoot.Count; ++i)
        {
            UICommonSlot slot = m_SlotPool.Get(i, false);
            if (slot == null)
                break;

            string itemID = m_ListLoot[i].Key;
            int count = m_ListLoot[i].Value;
            
            if (GameDatabase.S_TryGet(itemID, out DataDef def))
                slot.Set(itemID, def, count);
        }

        m_SlotPool.HideFrom(m_ListLoot.Count);
        m_SlotPool.Arrange(m_ListLoot.Count);
        m_EmptyLabel.SetActive(m_ListLoot.Count == 0);
    }

    // 창이 열린 뒤 주운 버섯을 하나씩 튀어나오게 보여 준다
    // !!) 전투가 끝날 때 timeScale이 멈춰 있을 수 있어 연출은 모두 unscaled 시간으로 돈다
    protected override IEnumerator CoAfterOpen()
    {
        for (int i = 0; i < m_SlotPool.Count && i < m_ListLoot.Count; ++i)
        {
            UICommonSlot slot = m_SlotPool.List[i];
            slot.gameObject.SetActive(true);
            
            Transform target = slot.transform;
            StartCoroutine(UITween.CoPlay(SlotPopTime, t => target.localScale = Vector3.one * UITween.EaseOutBack(t)));
            
            yield return new WaitForSecondsRealtime(m_SlotInterval);
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        m_RetryButton.interactable = isInteractable;
        m_LobbyButton.interactable = isInteractable;
    }

    // static ---

    // 시장에서 오른 상인 신뢰도. "  ·  신뢰도 약재상 +10, 요리사 +5"
    private static string FormatTrust(EBattleState state)
    {
        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        if (state != EBattleState.Exited || hunt == null || hunt.DicTrustGain.Count == 0)
            return string.Empty;

        System.Text.StringBuilder builder = new("  ·  신뢰도 ");
        bool isFirst = true;
        
        foreach (var pair in hunt.DicTrustGain)
        {
            if (GameDatabase.S_TryGet(pair.Key, out MerchantDef merchant) == false)
                continue;
            
            if (isFirst == false)
                builder.Append(", ");
            
            builder.Append($"{merchant.E_Alive()?.DisplayName ?? pair.Key} +{pair.Value}");
            isFirst = false;
        }

        return builder.ToString();
    }
}
