using UnityEngine;

// 토벌 입장 정보와 지역 해금 판정. 로비에서 고른 지역을 전투 씬이 읽을 수 있게 static으로 둔다.
// 전투 승리 시 BattleManager가 MarkCleared(SelectedRegion)를 불러 다음 지역을 연다
public static class HuntContext
{
    public static RegionDef SelectedRegion { get; private set; }

    // ==================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => SelectedRegion = null;

    public static void Select(RegionDef region)
    {
        SelectedRegion = region;
    }

    public static bool IsCleared(RegionDef region)
    {
        GameManager gameM = GameManager.Instance;
        if (region == null || gameM == null)
            return false;

        return gameM.Player.HasFlag(region.ClearFlag);
    }

    public static bool IsUnlocked(RegionDef region)
    {
        if (region == null)
            return false;
        if (region.RequireRegion == null)
            return true;

        return IsCleared(region.RequireRegion);
    }

    public static void MarkCleared(RegionDef region)
    {
        GameManager gameM = GameManager.Instance;
        if (region == null || gameM == null)
            return;

        gameM.Player.SetFlag(region.ClearFlag);
    }
}
