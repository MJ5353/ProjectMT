using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 전투 한 판의 유닛(플레이어·몬스터) 목록. 생성·삭제는 여기서만 한다(UnitFactory → ResourceManager 풀)
public sealed class UnitService : ServiceBase
{
    // 조회용
    private readonly Dictionary<int, UnitController> m_DicUnit = new();
    private readonly Dictionary<Transform, int> m_DicUnitID = new();
    
    // 순회용
    private readonly List<UnitController> m_ListUnit = new();
    private readonly List<int> m_ListToRemove = new();
    
    private readonly UnitFactory m_Factory = new();
    private bool m_IsTicking;
    private int m_LastUnitID;

    public event Action<int> UnitRemoved;
    public UnitController Player { get; private set; }

    public IReadOnlyList<UnitController> ListUnit => m_ListUnit; // 살아 있는 유닛 전부(HUD 체력바 등 읽기 전용)

    // ------------------------------------------------

    // Def의 프리팹(몸)으로 만들고 Def를 넘긴다
    public UnitController Spawn(UnitDef def, Vector3 position, Quaternion rotation)
    {
        if (def == null)
            return null;

        int unitID = ++m_LastUnitID;
        
        UnitController unit = m_Factory.Create(def, position, rotation, unitID);
        if (unit == null)
            return null;

        m_DicUnit.Add(unitID, unit);
        m_DicUnitID.Add(unit.transform, unitID);
        m_ListUnit.Add(unit);

        if (unit.Def is PlayerDef)
            Player = unit;

        return unit;
    }

    // 순회 도중 스폰은 목록 뒤에 붙어 이번 프레임엔 돌지 않고, 삭제는 모았다가 순회 뒤 처리한다
    public override void Tick(float deltaTime)
    {
        float unscaledDeltaTime = Time.unscaledDeltaTime;

        m_IsTicking = true;
        int count = m_ListUnit.Count;

        for (int i = 0; i < count; ++i)
        {
            m_ListUnit[i].Tick(deltaTime, unscaledDeltaTime);
        }

        m_IsTicking = false;
        
        foreach (int unitID in m_ListToRemove)
        {
            Despawn(unitID);
        }

        m_ListToRemove.Clear();
    }

    public void Despawn(int unitID)
    {
        if (m_IsTicking)
        {
            m_ListToRemove.Add(unitID);
            return;
        }

        if (m_DicUnit.Remove(unitID, out UnitController unit) == false)
            return;

        m_ListUnit.Remove(unit);

        if (unit != null)
            m_DicUnitID.Remove(unit.transform);

        if (ReferenceEquals(Player, unit))
            Player = null;

        UnitRemoved?.Invoke(unitID);
        m_Factory.Release(unit);
    }

    // 남은 유닛을 모두 풀로 돌려보낸다(다시 하기에 겹치지 않게). 판이 끝나는 정리라 UnitRemoved는 보내지 않는다.
    // 씬과 함께 이미 지워진 유닛은 건너뛴다(UnitFactory.Release가 null을 거른다)
    public override void Clear()
    {
        UnitRemoved = null;
        foreach (UnitController unit in m_ListUnit)
        {
            m_Factory.Release(unit);
        }

        m_DicUnit.Clear();
        m_DicUnitID.Clear();
        m_ListUnit.Clear();
        m_ListToRemove.Clear();
        
        Player = null;
    }

    public bool TryGetUnitID(Transform unitTransform, out int unitID)
    {
        unitID = 0;
        return unitTransform != null && m_DicUnitID.TryGetValue(unitTransform, out unitID);
    }

    // 주변 유닛 검색 ---

    public UnitController FindNearestEnemy(Vector3 position, float range, ETeam team)
    {
        UnitController nearest = null;
        float nearestDistanceSq = range * range;
        
        foreach (UnitController unit in m_DicUnit.Values)
        {
            if (IsEnemy(unit, team) == false)
                continue;

            float distanceSq = (unit.transform.position - position).sqrMagnitude;
            if (distanceSq >= nearestDistanceSq)
                continue;

            nearest = unit;
            nearestDistanceSq = distanceSq;
        }

        return nearest;
    }

    public void CollectNearbyEnemies(Vector3 center, float radius, ETeam team, List<UnitHitCandidate> results)
    {
        results.Clear();
        foreach (UnitController unit in m_DicUnit.Values)
        {
            CharacterController body = unit.HurtBody;
            if (IsEnemy(unit, team) == false || body == null || body.enabled == false)
                continue;

            UnitHitCandidate candidate = new(unit, body);
            if (candidate.IsNear(center, radius))
                results.Add(candidate);
        }
    }

    // 다른 몸(팀 무관)과 margin 안으로 붙어 있는지. 수평 거리 + 높이 범위만 본다 -> 겹치지 않기용
    public bool IsBodyNear(CharacterController self, float margin)
    {
        Vector3 position = self.transform.position;
        foreach (UnitController unit in m_DicUnit.Values)
        {
            CharacterController body = unit.HurtBody;
            if (body == null || body == self || body.enabled == false)
                continue;

            Vector3 otherPosition = body.transform.position;
            if (otherPosition.y > position.y + self.height + margin || position.y > otherPosition.y + body.height + margin)
                continue;

            Vector3 offset = otherPosition - position;
            offset.y = 0f;
            
            float range = self.radius + self.skinWidth + body.radius + margin;
            if (offset.sqrMagnitude < range * range)
                return true;
        }

        return false;
    }

    private static bool IsEnemy(UnitController unit, ETeam team)
    {
        if (unit == null)
            return false;

        // 숨은 적(땅속)과 펫은 노리지도 맞히지도 못한다
        return unit.IsDead == false && unit.Team != team && unit.HitF.IsHidden == false && unit.Def.IsUntargetable == false;
    }
}

// ==================================================

public readonly struct UnitHitCandidate
{
    public readonly UnitController Unit;
    public readonly Vector3 Center;
    public readonly Vector3 Axis;
    public readonly float HalfHeight;
    public readonly float Radius;

    // 몸 판정 표가 있으면 캡슐 대신 그것만 쓴다(버섯이 걸린 몸 부위도 몸 판정 표다)
    private bool HasBodyShape() => Unit.E_Alive()?.HitF?.HasShape ?? false;
    
    // ------------------------------------------------
    
    public UnitHitCandidate(UnitController unit, CharacterController body)
    {
        Transform bodyTransform = body.transform;
        Center = bodyTransform.TransformPoint(body.center);
        Axis = bodyTransform.up;
        
        Vector3 scale = bodyTransform.lossyScale;
        Radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        HalfHeight = Mathf.Max(Radius, body.height * Mathf.Abs(scale.y) * 0.5f);
        
        Unit = unit;
    }
    
    // 대충 가까운지. 뼈 모양이 있으면 감싸는 반지름, 없으면 CharacterController 캡슐 기준(부위만 있는 몹은 둘 다)
    public bool IsNear(Vector3 center, float radius)
    {
        float capsuleRange = radius + HalfHeight;
        if ((Center - center).sqrMagnitude <= capsuleRange * capsuleRange)
            return true;

        if (HasBodyShape() == false)
            return false;

        float shapeRange = radius + Unit.HitF.BoundRadius;
        return (Unit.transform.position - center).sqrMagnitude <= shapeRange * shapeRange;
    }

    // 공격 구와 겹치는지. 부위 → 몸 순서로 보고, 겹친 것 중 표면이 구 중심에 가장 가까운 모양.
    // 몸 뼈 모양이 없으면(일반 몹) 몸은 CharacterController 캡슐
    public bool OverlapsSphere(Vector3 center, float radius, out Vector3 point, out int partIndex)
    {
        if (TryOverlapShapes(center, radius, true, out point, out partIndex))
            return true;

        if (TryOverlapShapes(center, radius, false, out point, out partIndex))
            return true;

        partIndex = -1;
        
        if (HasBodyShape())
            return false;

        float halfAxis = HalfHeight - Radius;
        return HitMath.SphereVsCapsule(Center - Axis * halfAxis, Center + Axis * halfAxis, Radius, center, radius, out point, out float _);
    }
    
    private bool TryOverlapShapes(Vector3 center, float radius, bool isPart, out Vector3 point, out int partIndex)
    {
        point = default;
        partIndex = -1;
        bool isHit = false;
        float nearest = float.MaxValue;
        
        if (HasBodyShape() == false)
            return false;

        foreach (HitShape shape in Unit.HitF.ListShape)
        {
            if ((shape.PartIndex >= 0) != isPart)
                continue;

            shape.GetSegment(out Vector3 start, out Vector3 end);
            
            if (HitMath.SphereVsCapsule(start, end, shape.Radius, center, radius, out Vector3 shapePoint, out float distanceSq) == false)
                continue;

            // 표면까지 거리로 비교
            float distance = Mathf.Sqrt(distanceSq) - shape.Radius;
            if (distance >= nearest)
                continue;

            isHit = true;
            nearest = distance;
            point = shapePoint;
            partIndex = shape.PartIndex;
        }

        return isHit;
    }

}
