using System;
using System.Collections.Generic;
using UnityEngine;

// 액터가 공통으로 가져야 하는 것. ActorFactory가 풀에서 꺼낸 뒤 OnSpawn, 돌려보내기 전에 OnDespawn을 부른다
public interface IActor : IHitTarget
{
    int ActorID { get; }
    void OnSpawn(int actorID);
    void OnDespawn();
}

// 전투 한 판의 액터(플레이어·몬스터) 목록. 생성·삭제는 여기서만 한다(ActorFactory → ResourceManager 풀)
public sealed class ActorManagement
{
    private readonly ActorFactory m_Factory = new();
    private readonly Dictionary<int, IActor> m_DicActor = new();
    private readonly Dictionary<Transform, int> m_DicActorID = new();
    private int m_LastActorID;

    public PlayerController Player { get; private set; }

    public event Action<int> ActorRemoved;

    // ==================================================

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
    public void Clear()
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

    // 주변 액터 검색---

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
            float range = radius + candidate.HalfHeight;
            if ((candidate.Center - center).sqrMagnitude <= range * range)
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

    public bool OverlapsSphere(Vector3 center, float radius, out Vector3 point)
    {
        float halfAxis = HalfHeight - Radius;
        float alongAxis = Mathf.Clamp(Vector3.Dot(center - Center, Axis), -halfAxis, halfAxis);
        Vector3 axisPoint = Center + Axis * alongAxis;
        Vector3 offset = center - axisPoint;
        float distanceSq = offset.sqrMagnitude;
        float combinedRadius = radius + Radius;

        if (distanceSq > combinedRadius * combinedRadius)
        {
            point = default;
            return false;
        }

        point = distanceSq <= Radius * Radius ? center : axisPoint + offset.normalized * Radius;
        return true;
    }
}
