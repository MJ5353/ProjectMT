using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActorManagement
{
    private readonly Dictionary<int, IHitTarget> m_DicActor = new();
    private readonly Dictionary<Transform, int> m_DicActorID = new();
    private int m_LastActorID;

    public PlayerController Player { get; private set; }

    internal event Action<int> ActorRemoved;

    // ==================================================

    internal int Register(IHitTarget actor)
    {
        Transform actorTransform = actor.transform;
        if (m_DicActorID.TryGetValue(actorTransform, out int actorID))
            return actorID;

        actorID = ++m_LastActorID;
        m_DicActor.Add(actorID, actor);
        m_DicActorID.Add(actorTransform, actorID);
        if (actor is PlayerController player)
            Player = player;

        return actorID;
    }

    internal void Unregister(int actorID, Transform actorTransform)
    {
        if (m_DicActor.Remove(actorID, out IHitTarget actor) == false)
            return;

        m_DicActorID.Remove(actorTransform);
        if (ReferenceEquals(Player, actor))
            Player = null;

        ActorRemoved?.Invoke(actorID);
    }

    public bool TryGetActorID(Transform actorTransform, out int actorID)
    {
        actorID = 0;
        return actorTransform != null && m_DicActorID.TryGetValue(actorTransform, out actorID);
    }

    internal void Clear()
    {
        m_DicActor.Clear();
        m_DicActorID.Clear();
        Player = null;
        ActorRemoved = null;
    }

    // 주변 액터 검색---

    public IHitTarget FindNearestEnemy(Vector3 position, float range, ETeam team)
    {
        IHitTarget nearest = null;
        float nearestDistanceSq = range * range;
        foreach (IHitTarget actor in m_DicActor.Values)
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

    internal void CollectNearbyEnemies(Vector3 center, float radius, ETeam team, List<ActorHitCandidate> results)
    {
        results.Clear();
        foreach (IHitTarget actor in m_DicActor.Values)
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

internal readonly struct ActorHitCandidate
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
