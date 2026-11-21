using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 전투 결과 팝업. BattleEnded를 받은 ScreenBattle이 PopupService로 띄우고, BattleService에서 끝난 방식·시간·받은 보상(DicReward)을 읽어 보여 준다.
// [mj] todo) 신선 등급은 전투 쪽 데이터(ListLootGrade 가안)가 확정되면 칸 뱃지로 붙인다

public class PopupResult : PopupBase
{
    private const float SlotPopTime = 0.18f; // 칸 하나가 튀어나오는 시간

    [SerializeField] private UIListView<UICommonSlot> m_SlotPool = new(); // 주운 버섯 칸(격자)
    [SerializeField] private TMP_Text m_Title = null;
    [SerializeField] private TMP_Text m_Summary = null;
    [SerializeField] private GameObject m_EmptyLabel = null;
    [SerializeField] private Button m_RetryButton = null;
    [SerializeField] private Button m_LobbyButton = null;
    [SerializeField] private float m_SlotInterval = 0.08f;

    private readonly List<KeyValuePair<string, int>> m_ListLoot = new(); // 이번 결과에 보여 줄 보상(ID, 개수). m_SlotPool 순서

    public override bool IsCloseOnBack => false; // 다시 하기/로비로 중 하나를 골라야 한다

    // ------------------------------------------------

    private void Awake()
    {
        m_SlotPool.Init(EUIComponentType.UICommonSlot);
        m_RetryButton.E_SetOnClick(OnClickRetry);
        m_LobbyButton.E_SetOnClick(OnClickLobby);
    }

    // virtual ---

    protected override void OnOpen()
    {
        BattleService battleS = GameManager.Instance.BattleS;
        if (battleS == null)
            return;

        string title = battleS.State switch
        {
            EBattleState.Exited => "토벌 종료",
            EBattleState.TimedOut => "날이 밝았다",
            _ => "빈손으로 귀가",
        };
        m_Title.E_SetText(title);
        m_Title.E_SetColor(battleS.State == EBattleState.Exited ? UIStyle.Gold : UIStyle.Accent);
        m_Summary.E_SetText($"시간 {UINightClock.FormatRealTime(Mathf.FloorToInt(battleS.ElapsedTime))}{FormatTrust(battleS.State)}");

        FillLoot(battleS.DicReward);
        m_LobbyButton.E_SetActive(Application.CanStreamedLevelBeLoaded(SceneNames.Lobby));
        SetButtonsInteractable(true);
    }

    #region OnClick
    
    private void OnClickRetry()
    {
        BattleService battleS = GameManager.Instance.BattleS;
        if (battleS == null)
            return;

        // 씬 로드 중 두 번 눌리지 않게 막는다
        SetButtonsInteractable(false);
        battleS.Retry();
    }

    private void OnClickLobby()
    {
        BattleService battleS = GameManager.Instance.BattleS;
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

        // 칸은 꺼 둔 채 준비하고, 여는 연출 뒤에 하나씩 켠다(그래서 Refresh 대신 Get)
        for (int i = 0; i < m_ListLoot.Count; ++i)
        {
            UICommonSlot slot = m_SlotPool.Get(i, false);
            if (slot == null)
                break;

            string itemID = m_ListLoot[i].Key;
            int count = m_ListLoot[i].Value;
            
            GameDatabase.S_TryGet(itemID, out DataDef def); // 없으면 회색 칸 + ID
            slot.Set(itemID, def, count);
        }

        m_SlotPool.HideFrom(m_ListLoot.Count);
        m_SlotPool.Arrange(m_ListLoot.Count);
        m_EmptyLabel.E_SetActive(m_ListLoot.Count == 0);
    }

    // 창이 열린 뒤 주운 버섯을 하나씩 튀어나오게 보여 준다(튀어나오는 동안 다음 칸이 이어 나온다)
    protected override void OnBuildOpenSequence(UISequence sequence)
    {
        for (int i = 0; i < m_SlotPool.Count && i < m_ListLoot.Count; ++i)
        {
            UICommonSlot slot = m_SlotPool.List[i];
            Transform target = slot.transform;

            sequence.Call(() => slot.gameObject.SetActive(true))
                .Join(SlotPopTime, t => target.localScale = Vector3.one * UITween.EaseOutBack(t))
                .Wait(m_SlotInterval);
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        m_RetryButton.E_SetInteractable(isInteractable);
        m_LobbyButton.E_SetInteractable(isInteractable);
    }

    // static ---

    // 시장에서 오른 상인 신뢰도. "  ·  신뢰도 약재상 +10, 요리사 +5"
    private static string FormatTrust(EBattleState state)
    {
        HuntData hunt = PlayerDataManager.Instance.Hunt;
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
