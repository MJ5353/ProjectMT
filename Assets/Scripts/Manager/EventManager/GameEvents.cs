// 게임 전역 이벤트 모음. 새 이벤트는 readonly struct로 여기에 추가한다

// 씬의 Flow가 시작됨(공용·전용 Service의 Init이 끝난 뒤). FlowManager가 보낸다
public readonly struct FlowBegan
{
    public readonly FlowBase Flow;

    public FlowBegan(FlowBase flow)
    {
        Flow = flow;
    }
}

public readonly struct InventoryChanged
{
    public readonly string ID;
    public readonly int Count; // 바뀐 뒤 개수
    public readonly int Delta; // 증감량

    public InventoryChanged(string itemID, int count, int delta)
    {
        ID = itemID;
        Count = count;
        Delta = delta;
    }
}

public readonly struct FlagChanged
{
    public readonly string Flag;
    public readonly bool Value;

    public FlagChanged(string flag, bool value)
    {
        Flag = flag;
        Value = value;
    }
}

public readonly struct TabChanged
{
    public readonly int From;
    public readonly int To;

    public TabChanged(int from, int to)
    {
        From = from;
        To = to;
    }
}

// 시장 상태가 바뀜(상인, 남은 버섯, 남은 밤). 시장 팝업이 다시 그린다
public readonly struct MarketChanged
{
}

// 시장이 닫힘. 판 골드와 썩은 버섯은 이미 인벤토리에 들어갔다
public readonly struct MarketClosed
{
    public readonly int Gold;
    public readonly int RottenCount;

    public MarketClosed(int gold, int rottenCount)
    {
        Gold = gold;
        RottenCount = rottenCount;
    }
}

// 정비 강화 노드가 열림(디버그 초기화면 NodeID가 null). 정비 탭이 다시 그린다
public readonly struct GearChanged
{
    public readonly string NodeID;

    public GearChanged(string nodeID)
    {
        NodeID = nodeID;
    }
}
