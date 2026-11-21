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
    [SerializeField] private UIMapNode m_NodeTemplate = null;

    [Header("정보")]
    [SerializeField] private TMP_Text m_InfoName = null;
    [SerializeField] private TMP_Text m_InfoState = null;
    [SerializeField] private TMP_Text m_InfoDesc = null;
    [SerializeField] private RectTransform m_MushroomRoot = null;
    [SerializeField] private UICommonSlot m_MushroomTemplate = null;
    [SerializeField] private UITooltip m_MushroomTooltip = null; // 칸을 누르면 그 칸 위에 버섯 이름. 목록에 하나
    [SerializeField] private Button m_EnterButton = null;

    private readonly List<MapDef> m_ListMap = new();
    private readonly List<UIMapNode> m_ListNode = new();
    private readonly List<UICommonSlot> m_ListMushroomSlot = new();

    private MapDef m_Selected;

    // ==================================================

    private void Awake()
    {
        m_NodeTemplate.gameObject.SetActive(false);
        m_MushroomTemplate.gameObject.SetActive(false);
        m_EnterButton.onClick.AddListener(OnClickEnter);
    }

    // virtual ---

    protected override void OnOpen()
    {
        if (m_ListNode.Count == 0)
            BuildNodes();

        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        if (hunt == null)
            return;

        // 마지막에 고른 지역, 없으면 열린 지역 중 가장 뒤
        MapDef target = hunt.SelectedMap;
        if (target == null || m_ListMap.Contains(target) == false)
            target = m_ListMap.LastOrDefault(hunt.IsUnlocked);

        Select(target);
    }

    // ==================================================

    private void BuildNodes()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || gameM.Database == null)
            return;

        m_ListMap.AddRange(gameM.Database.GetAll<MapDef>().OrderBy(r => r.Order));

        foreach (MapDef mapDef in m_ListMap)
        {
            UIMapNode node = Instantiate(m_NodeTemplate, m_MapRoot);
            node.name = $"Node_{mapDef.ID}";
            node.gameObject.SetActive(true);

            RectTransform rt = (RectTransform)node.transform;
            rt.anchorMin = mapDef.MapPosition;
            rt.anchorMax = mapDef.MapPosition;
            rt.anchoredPosition = ((RectTransform)m_NodeTemplate.transform).anchoredPosition; // 지도 위치에서 원본 노드 위치만큼 띄운다(로비 씬 NodeTemplate에서 조절)

            node.Set(mapDef);

            MapDef captured = mapDef;
            node.Button.onClick.AddListener(() => Select(captured));
            m_ListNode.Add(node);
        }
    }

    private void Select(MapDef mapDef)
    {
        m_Selected = mapDef;
        RefreshNodes();
        RefreshInfo();
    }

    private void RefreshNodes()
    {
        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        if (hunt == null)
            return;

        for (int i = 0; i < m_ListNode.Count; i++)
        {
            MapDef mapDef = m_ListMap[i];
            UIMapNode node = m_ListNode[i];

            Color color = UIStyle.PanelLight;
            if (hunt.IsCleared(mapDef))
                color = UIStyle.Gold;
            else if (hunt.IsUnlocked(mapDef))
                color = UIStyle.Text; // 입장 가능: 평소 글자색(상아)

            node.SetState(color, hunt.IsUnlocked(mapDef));
            node.transform.localScale = mapDef == m_Selected ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    private void RefreshInfo()
    {
        foreach (UICommonSlot slot in m_ListMushroomSlot)
        {
            Destroy(slot.gameObject);
        }

        m_ListMushroomSlot.Clear();
        if (m_MushroomTooltip != null) // 로비 UI를 다시 만들기 전엔 비어 있다
            m_MushroomTooltip.Hide();

        if (m_Selected == null)
        {
            m_InfoName.text = "지역 없음";
            m_InfoState.text = string.Empty;
            m_InfoDesc.text = "MushTaker/데이터 편집기에서 MapDef를 준비하세요.";
            m_EnterButton.interactable = false;
            return;
        }

        m_InfoName.text = m_Selected.DisplayName;
        m_InfoDesc.text = m_Selected.Description;

        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        bool isUnlocked = hunt != null && hunt.IsUnlocked(m_Selected);
        if (hunt != null && hunt.IsCleared(m_Selected))
            m_InfoState.text = "토벌 완료";
        else if (isUnlocked)
            m_InfoState.text = "토벌 가능";
        else
            m_InfoState.text = $"잠김 · {m_Selected.RequireMap.DisplayName}을(를) 먼저 토벌";

        m_InfoState.color = isUnlocked ? UIStyle.Gold : UIStyle.TextDim;
        m_EnterButton.interactable = isUnlocked;

        foreach (MushroomDef mushroom in m_Selected.ListMushroom)
        {
            if (mushroom == null)
                continue;

            UICommonSlot slot = Instantiate(m_MushroomTemplate, m_MushroomRoot);
            slot.gameObject.SetActive(true);
            slot.Set(mushroom.ID, mushroom.Icon, string.Empty, 0, mushroom.Tint); // 이름은 칸 아래 대신 툴팁으로
            string displayName = mushroom.DisplayName;
            slot.SetOnClick(s => m_MushroomTooltip.Toggle((RectTransform)s.transform, displayName));
            m_ListMushroomSlot.Add(slot);
        }
    }

    private void OnClickEnter()
    {
        GameManager gameM = GameManager.Instance;
        HuntData hunt = PlayerDataManager.Instance?.Hunt;
        if (gameM == null || hunt == null || m_Selected == null || hunt.IsUnlocked(m_Selected) == false)
            return;

        hunt.Select(m_Selected);
        gameM.ChangeState(EGameState.Battle);
    }
}
