using UnityEngine;

// 복셀 조각 튀김 반응. 맞은 방향으로 조각을 흩뿌려 달라고 진행 중인 전투의 ChunkBurstManagement에 요청만 한다.
// 조각 움직임·그리기·재사용은 ChunkBurstManagement가 맡는다(게임 오브젝트 생성 없음)
public class ChunkBurstFeature : UnitFeature, IHitReaction
{
    private const float Height = 0.5f;   // 조각이 튀어나오는 높이

    private readonly Color m_Color;

    // ==================================================

    public ChunkBurstFeature(IActor owner, Color color) : base(owner)
    {
        m_Color = color;
    }

    // 전투 씬이 아니면 조각을 튀기지 않는다
    public void OnHit(in HitParams hit)
    {
        ChunkBurstManagement management = GameManager.Instance?.ActiveBattle?.Chunks;
        if (management == null)
            return;

        management.Burst(Owner.transform.position + Vector3.up * Height, hit.Direction, m_Color);
    }
}
