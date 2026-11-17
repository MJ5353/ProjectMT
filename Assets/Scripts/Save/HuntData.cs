
// 토벌 데이터. 로비에서 고른 지역(현재값)과 지역 해금 판정을 맡는다. 클리어 기록은 FlagData에 남긴다.
// 전투 승리 시 BattleManager가 MarkCleared(SelectedRegion)를 불러 다음 지역을 연다
public class HuntData : PlayerDataBase
{
    private readonly FlagData m_Flag;

    public RegionDef SelectedRegion { get; private set; }

    // ------------------------------------------------
    
    public HuntData(FlagData flag)
    {
        m_Flag = flag;
    }

    public override void Clear()
    {
        SelectedRegion = null;
    }

    public void Select(RegionDef region)
    {
        SelectedRegion = region;
    }

    public bool IsCleared(RegionDef region)
    {
        if (region == null)
            return false;

        return m_Flag.Has(region.ClearFlag);
    }

    public bool IsUnlocked(RegionDef region)
    {
        if (region == null)
            return false;
        if (region.RequireRegion == null)
            return true;

        return IsCleared(region.RequireRegion);
    }

    public void MarkCleared(RegionDef region)
    {
        if (region == null)
            return;

        m_Flag.Set(region.ClearFlag);
    }
}
