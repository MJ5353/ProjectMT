using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 토벌 탭. 왼쪽 지도에 지역 노드를 찍고, 고른 지역의 정보를 오른쪽에 보여 준 뒤 입장 버튼으로 전투에 들어간다
public class HuntTabView : TabView
{
    [Header("지도")]
    [SerializeField] private RectTransform m_MapRoot = null;
    [SerializeField] private Button m_NodeTemplate = null;

    [Header("정보")]
    [SerializeField] private TMP_Text m_InfoName = null;
    [SerializeField] private TMP_Text m_InfoState = null;
    [SerializeField] private TMP_Text m_InfoDesc = null;
    [SerializeField] private RectTransform m_MushroomRoot = null;
    [SerializeField] private UIItemSlot m_MushroomTemplate = null;
    [SerializeField] private Button m_EnterButton = null;

    private readonly List<RegionDef> m_ListRegion = new();
    private readonly List<Button> m_ListNode = new();
    private readonly List<UIItemSlot> m_ListMushroomSlot = new();

    private RegionDef m_Selected;

    // ==================================================

    private void Awake()
    {
        m_NodeTemplate.gameObject.SetActive(false);
        m_MushroomTemplate.gameObject.SetActive(false);
        m_EnterButton.onClick.AddListener(OnClickEnter);
    }

    // virtual ------------------------------------------------

    protected override void OnOpen()
    {
        if (m_ListNode.Count == 0)
            BuildNodes();

        // 마지막에 고른 지역, 없으면 열린 지역 중 가장 뒤
        RegionDef target = HuntContext.SelectedRegion;
        if (target == null || m_ListRegion.Contains(target) == false)
            target = m_ListRegion.LastOrDefault(HuntContext.IsUnlocked);

        Select(target);
    }

    // ==================================================

    private void BuildNodes()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || gameM.Database == null)
            return;

        m_ListRegion.AddRange(gameM.Database.GetAll<RegionDef>().OrderBy(r => r.Order));

        foreach (RegionDef region in m_ListRegion)
        {
            Button node = Instantiate(m_NodeTemplate, m_MapRoot);
            node.name = $"Node_{region.ID}";
            node.gameObject.SetActive(true);

            RectTransform rt = (RectTransform)node.transform;
            rt.anchorMin = region.MapPosition;
            rt.anchorMax = region.MapPosition;
            rt.anchoredPosition = Vector2.zero;

            node.GetComponentInChildren<TMP_Text>().text = region.DisplayName;

            RegionDef captured = region;
            node.onClick.AddListener(() => Select(captured));
            m_ListNode.Add(node);
        }
    }

    private void Select(RegionDef region)
    {
        m_Selected = region;
        RefreshNodes();
        RefreshInfo();
    }

    private void RefreshNodes()
    {
        for (int i = 0; i < m_ListNode.Count; i++)
        {
            RegionDef region = m_ListRegion[i];
            Button node = m_ListNode[i];

            Color color = UIStyle.PanelLight;
            if (HuntContext.IsCleared(region))
                color = UIStyle.Gold;
            else if (HuntContext.IsUnlocked(region))
                color = UIStyle.Accent;

            node.targetGraphic.color = color;
            node.transform.localScale = region == m_Selected ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    private void RefreshInfo()
    {
        foreach (UIItemSlot slot in m_ListMushroomSlot)
        {
            Destroy(slot.gameObject);
        }

        m_ListMushroomSlot.Clear();

        if (m_Selected == null)
        {
            m_InfoName.text = "지역 없음";
            m_InfoState.text = string.Empty;
            m_InfoDesc.text = "메뉴 MushTaker/UI/4로 샘플 지역을 만드세요.";
            m_EnterButton.interactable = false;
            return;
        }

        m_InfoName.text = m_Selected.DisplayName;
        m_InfoDesc.text = m_Selected.Description;

        bool isUnlocked = HuntContext.IsUnlocked(m_Selected);
        if (HuntContext.IsCleared(m_Selected))
            m_InfoState.text = "토벌 완료";
        else if (isUnlocked)
            m_InfoState.text = "토벌 가능";
        else
            m_InfoState.text = $"잠김 · {m_Selected.RequireRegion.DisplayName}을(를) 먼저 토벌";

        m_InfoState.color = isUnlocked ? UIStyle.Gold : UIStyle.TextDim;
        m_EnterButton.interactable = isUnlocked;

        foreach (MushroomDef mushroom in m_Selected.ListMushroom)
        {
            if (mushroom == null)
                continue;

            UIItemSlot slot = Instantiate(m_MushroomTemplate, m_MushroomRoot);
            slot.gameObject.SetActive(true);
            slot.Set(mushroom, 0);
            slot.SetBadge(null, Color.clear);
            m_ListMushroomSlot.Add(slot);
        }
    }

    private void OnClickEnter()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || m_Selected == null || HuntContext.IsUnlocked(m_Selected) == false)
            return;

        HuntContext.Select(m_Selected);
        gameM.Flow.ChangeState(EGameState.Battle);
    }
}
