
// 토벌 데이터. 로비에서 고른 지역(현재값)과 지역 해금 판정을 맡는다. 클리어 기록은 FlagData에 남긴다.
// 전투 승리 시 BattleService가 해당 지역의 클리어 플래그를 기록한다.
public class HuntData : PlayerDataBase
{
    private readonly FlagData m_Flag;

    public MapDef SelectedMap { get; private set; }

    // ------------------------------------------------

    public HuntData(FlagData flag)
    {
        m_Flag = flag;
    }

    public override void Clear()
    {
        SelectedMap = null;
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
