// 히트스톱 반응. 타격에 실린 시간만큼 HitStopManager에 멈춤을 요청한다.
public class HitStopFeature : UnitFeature, IHitReaction
{
    public HitStopFeature(IActor owner) : base(owner) { }

    // ==================================================

    public void OnHit(in HitParams hit)
    {
        HitStopManager.Instance?.Request(hit.HitStop);
    }
}
