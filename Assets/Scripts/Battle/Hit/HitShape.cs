using System;
using UnityEngine;

// ==================================================

public enum EHitAxis
{
    X,
    Y,
    Z,
}

// ==================================================

// 몸 판정 표 한 줄(MonsterDef). 뼈 위치에서 뼈 로컬 축 방향으로 Length만큼 뻗은 캡슐, Length 0이면 구
[Serializable]
public class HitShapeDef
{
    [field: SerializeField] public EBattlePartType Part { get; private set; } = EBattlePartType.Body; // 따라갈 부위 뼈(프리팹 Hitbox_*)

    [field: SerializeField] public float Radius { get; private set; } = 0.5f; // 반지름(m)

    [field: SerializeField] public float Length { get; private set; } = 0f; // 축 방향 길이(m). 음수면 반대로, 0이면 구

    [field: SerializeField] public EHitAxis Axis { get; private set; } = EHitAxis.Y; // 뼈 로컬 축. 기즈모로 방향 확인
}

// ==================================================

// 런타임 판정 모양. 주인이 Set에서 뼈를 한 번 찾아 만든다. 위치는 판정할 때마다 지금 뼈 위치로 계산
public readonly struct HitShape
{
    public readonly Transform Bone;
    public readonly float Radius;
    public readonly float Length;
    public readonly EHitAxis Axis;
    public readonly int PartIndex; // -1이면 몸, 0 이상은 부위 번호(MonsterDef.ListMushroom 칸)

    // ------------------------------------------------

    public HitShape(Transform bone, HitShapeDef def, int partIndex)
    {
        Bone = bone;
        Radius = def.Radius;
        Length = def.Length;
        Axis = def.Axis;
        PartIndex = partIndex;
    }

    // 부위(버섯) 판정 구
    public HitShape(Transform bone, float radius, int partIndex)
    {
        Bone = bone;
        Radius = radius;
        Length = 0f;
        Axis = EHitAxis.Y;
        PartIndex = partIndex;
    }

    // 캡슐 양 끝(월드). 구면 둘이 같다
    public void GetSegment(out Vector3 start, out Vector3 end)
    {
        start = Bone.position;
        end = start + GetAxis(Bone, Axis) * Length;
    }

    public static Vector3 GetAxis(Transform bone, EHitAxis axis)
    {
        switch (axis)
        {
            case EHitAxis.X:
                return bone.right;
            case EHitAxis.Z:
                return bone.forward;
            default:
                return bone.up;
        }
    }
}

// ==================================================

public static class HitMath
{
    // 캡슐(선분 start~end, 반지름)과 구가 겹치는지. 접점은 구 중심이 캡슐 안이면 구 중심, 아니면 캡슐 표면
    // distanceSq: 구 중심에서 캡슐 축까지 거리 제곱(여러 모양 중 가까운 것 고르기용)

    public static bool SphereVsCapsule(Vector3 start, Vector3 end, float capsuleRadius, Vector3 center, float radius,
        out Vector3 point, out float distanceSq)
    {
        Vector3 segment = end - start;
        float lengthSq = segment.sqrMagnitude;
        float t = lengthSq > 0.000001f ? Mathf.Clamp01(Vector3.Dot(center - start, segment) / lengthSq) : 0f;

        Vector3 axisPoint = start + segment * t;
        Vector3 offset = center - axisPoint;
        distanceSq = offset.sqrMagnitude;

        float combinedRadius = radius + capsuleRadius;
        if (distanceSq > combinedRadius * combinedRadius)
        {
            point = default;
            return false;
        }

        point = distanceSq <= capsuleRadius * capsuleRadius ? center : axisPoint + offset.normalized * capsuleRadius;
        return true;
    }
}