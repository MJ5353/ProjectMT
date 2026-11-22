using UnityEngine;

// ==================================================

// 몬스터 공격 예고(바닥 위험 범위). 흐린 범위 위에 진한 채움이 차오르고, 꽉 차는 순간 판정이 나간다.
// 원은 가운데서 바깥으로, 사각형(돌진 경로)은 뒤쪽 끝에서 앞으로 찬다. 꺼내고 돌려놓는 일은 EffectService가 한다

public class TelegraphController : PoolableMono
{
    private const float GroundOffset = 0.03f; // 바닥과 겹쳐 깜빡이지 않게 살짝 띄운다

    [field: SerializeField] public bool IsLine { get; private set; } = false; // true면 사각형(원점이 뒤쪽 끝), false면 원(원점이 가운데)

    [SerializeField] private Transform m_Area = null; // 흐린 전체 범위. 1x1 크기 기준
    [SerializeField] private Transform m_Fill = null; // 진한 채움

    // ------------------------------------------------

    // 원: position이 가운데, width가 지름. 사각형: position이 뒤쪽 끝, forward 쪽으로 length만큼
    public void SetShape(Vector3 position, Vector3 forward, float width, float length)
    {
        forward.y = 0f;
        Quaternion rotation = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
        
        transform.SetPositionAndRotation(position + Vector3.up * GroundOffset, rotation);
        transform.localScale = new Vector3(width, 1f, IsLine ? length : width);
    }

    // 0~1. 1이면 판정 순간
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (IsLine)
        {
            m_Fill.localScale = new Vector3(1f, 1f, progress);
            m_Fill.localPosition = new Vector3(0f, 0f, progress * 0.5f);
            return;
        }

        m_Fill.localScale = new Vector3(progress, 1f, progress);
    }

    // virtual ---

    public override void OnPoolSpawn()
    {
        m_Area.localPosition = IsLine ? new Vector3(0f, 0f, 0.5f) : Vector3.zero;
        SetProgress(0f);
    }
}
