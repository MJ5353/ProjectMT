using UnityEngine;

// ==================================================

// 대상(다른 팀 유닛) 찾기·기억·거리 재기. 누구를 노릴지는 판단 쪽(입력·AI)이 정하고, 여기는 찾고 재는 것만 한다.
// 몬스터는 대상을 기억해 쫓고(Target), 플레이어는 공격할 때마다 가까운 적 쪽으로 조준만 한다(GetAimDirection)

public class TargetFeature : UnitFeature
{
    private UnitController m_Target;

    // 노리는 적. 죽었거나 같은 팀이 되면 null
    public UnitController Target => IsValid(m_Target) ? m_Target : null;
    public Transform TargetTransform => Target != null ? m_Target.transform : null;
    public bool HasTarget => Target != null;

    // ------------------------------------------------

    public TargetFeature(UnitController owner) : base(owner) { }

    public override void Reset()
    {
        m_Target = null;
    }

    public override void Clear()
    {
        m_Target = null;
    }

    public void SetTarget(UnitController target)
    {
        if (IsValid(target))
            m_Target = target;
    }

    // 대상이 없거나 무효면 range 안에서 가장 가까운 적으로 바꾼다
    public void Refresh(float range)
    {
        if (HasTarget == false)
            m_Target = FindNearest(range);
    }

    // range 안에서 가장 가까운 적(기억하지 않음)
    public UnitController FindNearest(float range)
    {
        return GameManager.Instance?.ActorS?.FindNearestEnemy(Owner.transform.position, range, Owner.Team);
    }

    // get ---

    // 대상 쪽 수평 방향. 대상이 없으면 지금 앞
    public Vector3 GetDirection()
    {
        Transform self = Owner.transform;
        Transform target = TargetTransform;
        if (target == null)
            return self.forward;

        Vector3 diff = target.position - self.position;
        diff.y = 0f;
        return diff.sqrMagnitude > 0.0001f ? diff.normalized : self.forward;
    }

    // 내 캡슐 표면부터 대상까지 수평 거리. 피벗 기준이면 반지름이 MaxRange보다 큰 몬스터(보스)가 공격 거리에 못 들어온다
    public float GetDistance()
    {
        Transform target = TargetTransform;
        if (target == null)
            return float.MaxValue;

        Vector3 diff = target.position - Owner.transform.position;
        diff.y = 0f;
        float bodyRadius = Owner.HurtBody != null ? Owner.HurtBody.radius : 0f;
        return Mathf.Max(0f, diff.magnitude - bodyRadius);
    }

    public bool IsInRange(float range)
    {
        return HasTarget && GetDistance() <= range;
    }

    // range 안 가장 가까운 적 쪽, 없으면 fallback(입력 방향 등)
    public Vector3 GetAimDirection(float range, Vector3 fallback)
    {
        UnitController nearest = FindNearest(range);
        if (nearest == null)
            return fallback;

        return nearest.transform.position - Owner.transform.position;
    }

    // ==================================================

    // 살아 있는 다른 팀인지(풀로 돌아갔거나 죽었으면 false)
    private bool IsValid(UnitController target)
    {
        return target != null && target.IsDead == false && target.Team != Owner.Team;
    }
}
