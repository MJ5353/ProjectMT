using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 액터가 공통으로 가져야 하는 것. ActorFactory가 풀에서 꺼낸 뒤 OnSpawn, 돌려보내기 전에 OnDespawn을 부른다
public interface IActor : IHitTarget
{
    int ActorID { get; }
    void OnSpawn(int actorID);
    void OnDespawn();
}

// ==================================================

// 전투 한 판의 액터(플레이어·몬스터) 목록. 생성·삭제는 여기서만 한다(ActorFactory → ResourceManager 풀)
public sealed class ActorService : ServiceBase
{
    private readonly ActorFactory m_Factory = new();
    private readonly Dictionary<int, IActor> m_DicActor = new();
    private readonly Dictionary<Transform, int> m_DicActorID = new();
    private int m_LastActorID;

    public PlayerController Player { get; private set; }

    public event Action<int> ActorRemoved;

    // ------------------------------------------------

    public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : PoolableMono, IActor
    {
        if (prefab == null)
            return null;

        int actorID = ++m_LastActorID;
        T actor = m_Factory.Create(prefab, position, rotation, actorID);
        if (actor == null)
            return null;

        m_DicActor.Add(actorID, actor);
        m_DicActorID.Add(actor.transform, actorID);

        if (actor is PlayerController player)
            Player = player;

        return actor;
    }

    public void Despawn(int actorID)
    {
        if (m_DicActor.Remove(actorID, out IActor actor) == false)
            return;

        if (actor is Component component && component != null)
            m_DicActorID.Remove(component.transform);

        if (ReferenceEquals(Player, actor))
            Player = null;

        ActorRemoved?.Invoke(actorID);
        m_Factory.Release(actor);
    }

    // 씬이 닫히며 남은 액터가 함께 지워지므로 목록만 비운다
    public override void Clear()
    {
        m_DicActor.Clear();
        m_DicActorID.Clear();
        Player = null;
        ActorRemoved = null;
    }

    public bool TryGetActorID(Transform actorTransform, out int actorID)
    {
        actorID = 0;
        return actorTransform != null && m_DicActorID.TryGetValue(actorTransform, out actorID);
    }

    // 주변 액터 검색 ---

    public IHitTarget FindNearestEnemy(Vector3 position, float range, ETeam team)
    {
        IHitTarget nearest = null;
        float nearestDistanceSq = range * range;
        foreach (IActor actor in m_DicActor.Values)
        {
            if (IsEnemy(actor, team) == false)
                continue;

            float distanceSq = (actor.transform.position - position).sqrMagnitude;
            if (distanceSq >= nearestDistanceSq)
                continue;

            nearest = actor;
            nearestDistanceSq = distanceSq;
        }

        return nearest;
    }

    public void CollectNearbyEnemies(Vector3 center, float radius, ETeam team, List<ActorHitCandidate> results)
    {
        results.Clear();
        foreach (IActor actor in m_DicActor.Values)
        {
            CharacterController body = actor.HurtBody;
            if (IsEnemy(actor, team) == false || body == null || body.enabled == false)
                continue;

            ActorHitCandidate candidate = new(actor, body);
            if (candidate.IsNear(center, radius))
                results.Add(candidate);
        }
    }

    private static bool IsEnemy(IHitTarget actor, ETeam team)
    {
        if (actor is UnityEngine.Object behaviour && behaviour == null)
            return false;

        return actor.HitFeature.IsDead == false && actor.HitFeature.Team != team;
    }
}

// ==================================================

public readonly struct ActorHitCandidate
{
    public readonly IHitTarget Actor;
    public readonly Vector3 Center;
    public readonly Vector3 Axis;
    public readonly float HalfHeight;
    public readonly float Radius;

    public ActorHitCandidate(IHitTarget actor, CharacterController body)
    {
        Actor = actor;
        Transform bodyTransform = body.transform;
        Vector3 scale = bodyTransform.lossyScale;
        Center = bodyTransform.TransformPoint(body.center);
        Axis = bodyTransform.up;
        Radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        HalfHeight = Mathf.Max(Radius, body.height * Mathf.Abs(scale.y) * 0.5f);
    }

    // 대충 가까운지. 뼈 모양이 있으면 감싸는 반지름, 없으면 CharacterController 캡슐 기준
    public bool IsNear(Vector3 center, float radius)
    {
        bool hasShape = Actor.ListHitShape.Count > 0;
        Vector3 bodyCenter = hasShape ? Actor.transform.position : Center;
        
        float range = radius + (hasShape ? Actor.HitBoundRadius : HalfHeight);
        return (bodyCenter - center).sqrMagnitude <= range * range;
    }

    // 공격 구와 겹치는지. 뼈 모양이 있으면 겹친 것 중 구 중심에 가장 가까운 모양, 없으면 CharacterController 캡슐
    public bool OverlapsSphere(Vector3 center, float radius, out Vector3 point, out int partIndex)
    {
        IReadOnlyList<HitShape> listShape = Actor.ListHitShape;
        partIndex = -1;
        if (listShape.Count == 0)
        {
            float halfAxis = HalfHeight - Radius;
            return HitMath.SphereVsCapsule(Center - Axis * halfAxis, Center + Axis * halfAxis, Radius, center, radius, out point, out float _);
        }

        point = default;
        bool isHit = false;
        float nearest = float.MaxValue;
        
        foreach (HitShape shape in listShape)
        {
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
