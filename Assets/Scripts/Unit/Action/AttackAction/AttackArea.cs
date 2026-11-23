using UnityEngine;

// ==================================================

// 공격 하나의 이번 프레임 범위. 일반 공격은 판정 구 하나(원), 돌진은 그 구가 Length만큼 앞으로 쓸고 가는 자리(사각형).
// 바닥 예고·판정 효과·퍼펙트 닷지 범위 기록이 모두 이것 하나를 읽어, 눈에 보이는 범위와 판정이 어긋나지 않게 한다.
// 높이는 판정 구 중심(Center)과 바닥(Ground)을 따로 든다. 범위 안인지는 바닥 평면에서만 본다

public readonly struct AttackArea
{
    public readonly Vector3 Center;   // 판정 구 중심
    public readonly Vector3 Ground;   // 판정 구 중심을 주인 발 높이로 내린 자리(바닥 예고·땅 이펙트)
    public readonly Vector3 Forward;  // 평면 앞 방향(길이 1)
    public readonly float Radius;     // 판정 반지름
    public readonly float Length;     // 판정 구가 앞으로 더 쓸고 갈 거리. 원이면 0

    public Vector3 BoundCenter => Center + Forward * (Length * 0.5f); // 후보 모으기용. 범위 전체를 감싸는 구
    public float BoundRadius => Radius + Length * 0.5f;

    // ------------------------------------------------

    public AttackArea(Vector3 center, float groundY, Vector3 forward, float radius, float length)
    {
        forward.y = 0f;

        Center = center;
        Ground = new Vector3(center.x, groundY, center.z);
        Forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        Radius = radius;
        Length = Mathf.Max(0f, length);
    }

    // 몸(바닥 위치 position, 반지름 bodyRadius)이 범위 안이면 true.
    // distance: 판정 구가 몸에 닿기까지 앞으로 더 가야 하는 거리(0 이하면 지금 자리에서 닿는다)
    public bool Contains(Vector3 position, float bodyRadius, out float distance)
    {
        float reach = Radius + bodyRadius;
        Vector3 offset = position - Center;
        offset.y = 0f;

        float along = Vector3.Dot(offset, Forward);
        Vector3 side = offset - Forward * along;
        distance = along - reach;

        if (Length <= 0f)
            return offset.sqrMagnitude <= reach * reach;

        return side.sqrMagnitude <= reach * reach && along >= -reach && along <= Length + reach;
    }

    // 판정 구 중심이 이 자리를 앞 방향으로 지나갔는지
    public bool HasPassed(Vector3 position)
    {
        Vector3 offset = position - Center;
        offset.y = 0f;

        return Vector3.Dot(offset, Forward) <= 0f;
    }
}
