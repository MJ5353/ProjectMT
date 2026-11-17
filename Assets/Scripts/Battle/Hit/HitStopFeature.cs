using UnityEngine;

// 히트스톱 반응. 타격에 실린 시간만큼 HitStopManager에 멈춤을 요청한다.
public class HitStopFeature : MonoBehaviour, IHitReaction
{
    [SerializeField, Tooltip("타격 값에 곱하는 배율")] private float m_Scale = 1f;

    public int Order => 30;

    public void OnHit(in HitParams hit)
    {
        HitStopManager.Request(hit.HitStop * m_Scale);
    }
}
