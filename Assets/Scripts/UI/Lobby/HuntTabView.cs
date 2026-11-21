using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==================================================

// 토벌 탭. 왼쪽 지도에 지역 노드를 찍고, 고른 지역의 정보를 오른쪽에 보여 준 뒤 입장 버튼으로 전투에 들어간다.
// 나오는 버섯 칸은 UIListView로 꺼내 쓴다(씬이 바뀌면 UIFactory가 회수)

public class HuntTabView : TabView
{
    // 지도 ---
    [SerializeField] private RectTransform m_MapRoot = null;
    [SerializeField] private UIMapNode m_NodeTemplate = null;

    // 정보 ---
    [SerializeField] private TMP_Text m_InfoName = null;
    [SerializeField] private TMP_Text m_InfoState = null;
    [SerializeField] private TMP_Text m_InfoDesc = null;
    [SerializeField] private UITooltip m_MushroomTooltip = null; // 칸을 누르면 그 칸 위에 버섯 이름. 목록에 하나
    [SerializeField] private Button m_EnterButton = null;
    [SerializeField] private UIListView<UICommonSlot> m_MushroomPool = new(); // 나오는 버섯 칸(가로)

    private readonly List<MapDef> m_ListMap = new();
    private readonly List<UIMapNode> m_ListNode = new();
    private readonly List<MushroomDef> m_ListMushroom = new(); // 고른 지역에서 나오는 버섯. m_MushroomPool 순서
    
    private MapDef m_Selected;

    // ------------------------------------------------

    private void Awake()
    {
        m_MushroomPool.Init(EUIComponentType.UICommonSlot, OnSpawnMushroomSlot);
        m_NodeTemplate.E_SetActive(false);
        m_EnterButton.E_SetOnClick(OnClickEnter);
    }

    // virtual ---

    protected override void OnOpen()
    {
        if (m_ListNode.Count == 0)
            BuildNodes();

        HuntData hunt = PlayerDataManager.Instance.Hunt;
        if (hunt == null)
            return;

        // 마지막에 고른 지역, 없으면 열린 지역 중 가장 뒤
        MapDef target = hunt.SelectedMap;
        if (target == null || m_ListMap.Contains(target) == false)
            target = m_ListMap.LastOrDefault(hunt.IsUnlocked);

        Select(target);
    }

    // ---

    private void BuildNodes()
    {
        m_ListMap.AddRange(GameManager.Instance.Database.GetAll<MapDef>().OrderBy(r => r.Order));

        RectTransform templateRt = m_NodeTemplate.transform as RectTransform;
        if (templateRt == null)
            return;

        foreach (MapDef mapDef in m_ListMap)
        {
            UIMapNode node = ResourceManager.S_Create(m_NodeTemplate, m_MapRoot);
            if (node == null)
                continue;

            node.name = $"Node_{mapDef.ID}";
            node.gameObject.SetActive(true);

            RectTransform rt = node.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = mapDef.MapPosition;
                rt.anchorMax = mapDef.MapPosition;
                rt.anchoredPosition = templateRt.anchoredPosition; // 지도 위치에서 원본 노드 위치만큼 띄운다(로비 씬 NodeTemplate에서 조절)
            }

            node.Set(mapDef);
            MapDef captured = mapDef;
            node.Button.E_SetOnClick(() => Select(captured));
            
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
        HuntData hunt = PlayerDataManager.Instance.Hunt;
        if (hunt == null)
            return;

        for (int i = 0; i < m_ListNode.Count; i++)
        {
            MapDef mapDef = m_ListMap.E_GetOrDefault(i);
            UIMapNode node = m_ListNode.E_GetOrDefault(i);

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
        if (m_MushroomTooltip != null) // 로비 UI를 다시 만들기 전엔 비어 있다
            m_MushroomTooltip.Hide();

        m_ListMushroom.Clear();
        
        if (m_Selected == null)
        {
            m_MushroomPool.HideFrom(0);
            m_InfoName.E_SetText("지역 없음");
            m_InfoState.E_SetText(string.Empty);
            m_InfoDesc.E_SetText("MushTaker/데이터 편집기에서 MapDef를 준비하세요.");
            m_EnterButton.E_SetInteractable(false);
            return;
        }

        m_InfoName.E_SetText(m_Selected.DisplayName);
        m_InfoDesc.E_SetText(m_Selected.Description);

        HuntData hunt = PlayerDataManager.Instance.Hunt;
        bool isUnlocked = hunt != null && hunt.IsUnlocked(m_Selected);
        
        if (hunt != null && hunt.IsCleared(m_Selected))
            m_InfoState.E_SetText("토벌 완료");
        else if (isUnlocked)
            m_InfoState.E_SetText("토벌 가능");
        else
            m_InfoState.E_SetText($"잠김 · {m_Selected.RequireMap.DisplayName}을(를) 먼저 토벌");

        m_InfoState.E_SetColor(isUnlocked ? UIStyle.Gold : UIStyle.TextDim);
        m_EnterButton.E_SetInteractable(isUnlocked);

        foreach (MushroomDef mushroom in m_Selected.ListMushroom)
        {
            if (mushroom != null)
                m_ListMushroom.Add(mushroom);
        }

        m_MushroomPool.Refresh(m_ListMushroom, SetMushroomSlot);
    }

    // 이름은 칸 아래 대신 툴팁으로 보여 준다
    private static void SetMushroomSlot(UICommonSlot slot, MushroomDef mushroom)
    {
        slot.Set(mushroom.ID, mushroom, 0, string.Empty);
    }

    private void OnSpawnMushroomSlot(UICommonSlot slot)
    {
        slot.SetOnClick(OnClickMushroom);
    }

    private void OnClickMushroom(UICommonSlot slot)
    {
        MushroomDef def = m_ListMushroom.E_GetOrDefault(m_MushroomPool.IndexOf(slot));
        if (def == null || m_MushroomTooltip == null)
            return;

        m_MushroomTooltip.Toggle(slot.RectTransform, def.DisplayName);
    }

    private void OnClickEnter()
    {
        if (m_Selected == null)
            return;
        
        HuntData hunt = PlayerDataManager.Instance.Hunt;
        if (hunt == null || hunt.IsUnlocked(m_Selected) == false)
            return;

        hunt.Select(m_Selected);
        GameManager.Instance.ChangeState(EGameState.Battle);
    }
}
