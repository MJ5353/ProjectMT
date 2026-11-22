
// ==================================================

// 토벌 데이터. 로비에서 고른 지역(현재값)과 지역 해금 판정, 토벌을 끝낸 뒤 현장 시장(하룻밤 임시값)을 맡는다. 클리어 기록은 FlagData에 남긴다.
// 전투 승리 시 BattleService가 해당 지역의 클리어 플래그를 기록한다.
// 시장 부분은 HuntData.Market.cs(partial)에 있다
public partial class HuntData : PlayerDataBase
{
    private readonly FlagData m_Flag;
    private readonly InventoryData m_Inventory; // 시장이 닫힐 때 골드·썩은 버섯을 넣는다
    private readonly MarketData m_Market;       // 상인 신뢰도(저장값)

    public MapDef SelectedMap { get; private set; }

    // ------------------------------------------------

    public HuntData(FlagData flag, InventoryData inventory, MarketData market)
    {
        m_Flag = flag;
        m_Inventory = inventory;
        m_Market = market;
    }

    public override void Clear()
    {
        SelectedMap = null;
        ClearMarket();
    }

    public void Select(MapDef mapDef)
    {
        SelectedMap = mapDef;
    }

    public bool IsCleared(MapDef mapDef)
    {
        if (mapDef == null)
            return false;

        return m_Flag.Has(mapDef.ClearFlag);
    }

    public bool IsUnlocked(MapDef mapDef)
    {
        if (mapDef == null)
            return false;
        if (mapDef.RequireMap == null)
            return true;

        return IsCleared(mapDef.RequireMap);
    }

    public void MarkCleared(MapDef mapDef)
    {
        if (mapDef == null)
            return;

        m_Flag.Set(mapDef.ClearFlag);
    }
}
