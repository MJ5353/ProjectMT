using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 정비 탭. 왼쪽 엠블렘(연 노드 수에 따른 단계·보너스), 가운데 강화 노드 트리, 오른쪽 고른 노드 정보와 강화 버튼.
// 노드·선은 처음 열 때 템플릿을 복제해 GearNodeDef.Slot 자리에 놓는다(토벌 탭 지도 노드와 같은 방식).
// 데이터는 GearData가 들고, 여기서는 TryOpen 요청만 한 뒤 GearChanged를 받아 다시 그린다

public class ScreenLobby_GearView : UITabView
{
    private const float MaxColumn = 8f;   // Slot.x 최대(열 0~8)
    private const float MaxRow = 5f;      // Slot.y 최대(행 0~5, 0이 아래)
    private const float LineWidth = 6f;

    // 엠블렘 ---
    [SerializeField] private Image m_EmblemImage = null;
    [SerializeField] private TMP_Text m_EmblemName = null;
    [SerializeField] private TMP_Text m_EmblemProgress = null;
    [SerializeField] private TMP_Text m_EmblemBonus = null;

    // 트리 ---
    [SerializeField] private RectTransform m_NodeRoot = null;    // 노드 칸 영역. 열·행을 이 안에 고르게 편다
    [SerializeField] private ScreenLobby_GearView_Node m_NodeTemplate = null;
    [SerializeField] private Image m_LineTemplate = null;

    // 정보 ---
    [SerializeField] private TMP_Text m_DetailName = null;
    [SerializeField] private TMP_Text m_DetailEffect = null;
    [SerializeField] private TMP_Text m_DetailRequire = null;
    [SerializeField] private TMP_Text m_DetailCost = null;
    [SerializeField] private Button m_OpenButton = null;
    [SerializeField] private TMP_Text m_OpenLabel = null;

    private readonly List<ScreenLobby_GearView_Node> m_ListNode = new();
    private readonly List<(Image Line, GearNodeDef From, GearNodeDef To)> m_ListLine = new();
    private readonly Dictionary<EGearStat, float> m_DicBonus = new(); // 엠블렘 보너스 합산용
    private readonly StringBuilder m_Builder = new();

    private ScreenLobby_GearView_Node m_Selected;

    private static GearData Gear => PlayerDataManager.Instance.Gear;

    // ------------------------------------------------

    private void Awake()
    {
        m_NodeTemplate.E_SetActive(false);
        m_LineTemplate.E_SetActive(false);
        m_OpenButton.E_SetOnClick(OnClickOpen);
    }

    // virtual ---

    protected override void OnOpen()
    {
        if (m_ListNode.Count == 0)
            BuildTree();

        SEventManager.Subscribe<GearChanged>(OnGearChanged);
        SEventManager.Subscribe<InventoryChanged>(OnInventoryChanged);

        if (m_Selected == null)
            m_Selected = m_ListNode.FirstOrDefault(node => Gear.CheckOpen(node.Node) != EGearOpenResult.Opened && Gear.IsReachable(node.Node)) ?? m_ListNode.FirstOrDefault();

        Refresh();
    }

    protected override void OnClose()
    {
        SEventManager.Unsubscribe<GearChanged>(OnGearChanged);
        SEventManager.Unsubscribe<InventoryChanged>(OnInventoryChanged);
    }

    // ==================================================

    // 선을 먼저 깔고 노드를 그 위에 놓는다. 자리는 노드 영역 크기 기준 비율
    private void BuildTree()
    {
        Vector2 size = m_NodeRoot.rect.size;
        IReadOnlyList<GearNodeDef> listNodeDef = Gear.ListNode;

        foreach (GearNodeDef nodeDef in listNodeDef)
        {
            foreach (GearNodeDef require in nodeDef.ListRequire)
            {
                if (require != null)
                    m_ListLine.Add((CreateLine(GetPosition(require, size), GetPosition(nodeDef, size)), require, nodeDef));
            }
        }

        foreach (GearNodeDef nodeDef in listNodeDef)
        {
            ScreenLobby_GearView_Node node = ResourceManager.S_Create(m_NodeTemplate, m_NodeRoot);
            node.name = $"Node_{nodeDef.ID}";
            node.E_SetActive(true);

            RectTransform rt = node.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = GetPosition(nodeDef, size);
            }

            node.Set(nodeDef);
            node.Button.E_SetOnClick(() => Select(node));
            m_ListNode.Add(node);
        }
    }

    private Image CreateLine(Vector2 from, Vector2 to)
    {
        Image line = ResourceManager.S_Create(m_LineTemplate, m_NodeRoot);
        line.E_SetActive(true);

        RectTransform rt = line.rectTransform;
        Vector2 delta = to - from;
        
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (from + to) * 0.5f;
        rt.sizeDelta = new Vector2(delta.magnitude, LineWidth);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        
        return line;
    }

    // 노드 영역 가운데 기준 좌표. 열 0~8을 가로로, 행 0~5를 아래에서 위로 편다
    private static Vector2 GetPosition(GearNodeDef node, Vector2 size)
    {
        float x = (node.Slot.x / MaxColumn - 0.5f) * size.x;
        float y = (node.Slot.y / MaxRow - 0.5f) * size.y;
        
        return new Vector2(x, y);
    }

    private void Select(ScreenLobby_GearView_Node node)
    {
        m_Selected = node;
        Refresh();
    }

    private void OnClickOpen()
    {
        if (m_Selected == null)
            return;

        Gear.TryOpen(m_Selected.Node); // 열리면 GearChanged로 다시 그린다
    }

    private void OnGearChanged(GearChanged evt) => Refresh();

    private void OnInventoryChanged(InventoryChanged evt)
    {
        if (evt.ID == CurrencyDef.GoldID)
            RefreshDetail();
    }

    // refresh ---

    private void Refresh()
    {
        RefreshTree();
        RefreshEmblem();
        RefreshDetail();
    }

    private void RefreshTree()
    {
        foreach (ScreenLobby_GearView_Node node in m_ListNode)
        {
            node.SetState(GetState(node.Node), node == m_Selected);
        }

        // 열린 노드로 이어진 선은 밝게, 앞 노드만 열린 선은 흐리게
        foreach (var (line, from, to) in m_ListLine)
        {
            Color color = Gear.IsOpened(to) ? UIStyle.Positive : Gear.IsOpened(from) ? UIStyle.TextDim : UIStyle.PanelLight;
            line.E_SetColor(color);
        }
    }

    private void RefreshEmblem()
    {
        GearData gear = Gear;
        EmblemDef emblem = gear.GetEmblem();
        EmblemDef next = gear.GetNextEmblem();

        if (emblem != null)
        {
            m_EmblemImage.E_SetImage(emblem.Icon, emblem.Tint);
            m_EmblemName.E_SetText(emblem.DisplayName);
        }

        m_Builder.Clear();
        m_Builder.Append(StringDef.S_Format(EStringKey.Lobby_Gear_EmblemProgressFormat, gear.OpenedCount, gear.TotalCount));
        m_Builder.Append('\n');
        m_Builder.Append(next != null ? 
            StringDef.S_Format(EStringKey.Lobby_Gear_EmblemNextFormat, next.NeedCount - gear.OpenedCount) : 
            StringDef.S_Get(EStringKey.Lobby_Gear_EmblemMax));
        
        m_EmblemProgress.E_SetText(m_Builder.ToString());

        gear.CollectEmblemBonus(m_DicBonus); // 지난 단계까지 능력치별 합

        m_Builder.Clear();
        foreach (var pair in m_DicBonus)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');

            m_Builder.Append(GetStatText(pair.Key, pair.Value));
        }

        m_EmblemBonus.E_SetText(m_Builder.Length > 0 ? m_Builder.ToString() : StringDef.S_Get(EStringKey.Lobby_Gear_NoBonus));
    }

    private void RefreshDetail()
    {
        if (m_Selected == null)
        {
            m_DetailName.E_SetString(EStringKey.Lobby_Gear_SelectHint);
            m_DetailEffect.E_SetText(string.Empty);
            m_DetailRequire.E_SetText(string.Empty);
            m_DetailCost.E_SetText(string.Empty);
            m_OpenButton.E_SetInteractable(false);
            return;
        }

        GearNodeDef node = m_Selected.Node;
        m_DetailName.E_SetText(node.DisplayName);

        m_Builder.Clear();
        
        // 효과: 능력치 줄 + 스페셜 설명(DataDef.Description)
        if (node.Stat != EGearStat.None)
            m_Builder.Append(GetStatText(node.Stat, node.StatValue));

        if (node.Description.E_IsNullOrEmpty() == false)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');

            m_Builder.Append(UIStyle.ColorTag(UIStyle.Gold, node.Description));
        }

        m_DetailEffect.E_SetText(m_Builder.ToString());
        m_DetailRequire.E_SetText(GetRequireText(node));

        EGearOpenResult result = Gear.CheckOpen(node);
        string cost = StringDef.S_Format(EStringKey.Lobby_Gear_CostFormat, node.Cost);
        m_DetailCost.E_SetTextWithActive(result == EGearOpenResult.NotEnoughGold ? UIStyle.ColorTag(UIStyle.Negative, cost) : cost, result != EGearOpenResult.Opened);

        m_OpenButton.E_SetInteractable(result == EGearOpenResult.Ok);
        m_OpenLabel.E_SetString(GetOpenKey(result));
    }

    // get ---

    private static EGearNodeState GetState(GearNodeDef node)
    {
        if (Gear.IsOpened(node))
            return EGearNodeState.Opened;

        return Gear.IsReachable(node) ? EGearNodeState.Openable : EGearNodeState.Locked;
    }

    // 앞 노드 이름. 열린 것은 초록, 아닌 것은 빨강
    private string GetRequireText(GearNodeDef node)
    {
        if (node.ListRequire.Count == 0)
            return StringDef.S_Get(EStringKey.Lobby_Gear_RequireNone);

        StringBuilder builder = new StringBuilder();
        foreach (GearNodeDef require in node.ListRequire)
        {
            if (require == null)
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(UIStyle.ColorTag(Gear.IsOpened(require) ? UIStyle.Positive : UIStyle.Negative, require.DisplayName));
        }

        return StringDef.S_Format(EStringKey.Lobby_Gear_RequireFormat, builder.ToString());
    }

    private static EStringKey GetOpenKey(EGearOpenResult result)
    {
        switch (result)
        {
            case EGearOpenResult.Opened:
                return EStringKey.Lobby_Gear_Opened;
            case EGearOpenResult.Locked:
                return EStringKey.Lobby_Gear_Locked;
            case EGearOpenResult.NotEnoughGold:
                return EStringKey.Lobby_Gear_NotEnoughGold;
            default:
                return EStringKey.Lobby_Gear_Open;
        }
    }

    // 능력치 한 줄. 값은 비율이라 %로, 경직 저항만 수치로
    private static string GetStatText(EGearStat stat, float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        switch (stat)
        {
            case EGearStat.Attack:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_Attack, percent);
            case EGearStat.MaxHp:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_MaxHp, percent);
            case EGearStat.Dodge:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_Dodge, percent);
            case EGearStat.Defense:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_Defense, percent);
            case EGearStat.AttackSpeed:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_AttackSpeed, percent);
            case EGearStat.MoveSpeed:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_MoveSpeed, percent);
            case EGearStat.PartDamage:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_PartDamage, percent);
            case EGearStat.DashDistance:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_DashDistance, percent);
            case EGearStat.BreakResist:
                return StringDef.S_Format(EStringKey.Lobby_Gear_Stat_BreakResist, value);
            default:
                return string.Empty;
        }
    }
}
