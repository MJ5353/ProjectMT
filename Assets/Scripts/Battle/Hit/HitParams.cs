using UnityEngine;

public enum ETeam
{
    Player,
    Mob,
}

// 한 번의 타격 정보. 공격하는 쪽이 만들고, 맞는 쪽 HitFeature가 반응 부품들에 그대로 넘긴다.
public readonly struct HitParams
{
    public readonly GameObject Attacker;
    public readonly ETeam Team;
    public readonly int Damage;
    public readonly Vector3 Direction; // 수평, 정규화
    public readonly float Knockback;
    public readonly float HitStop;
    public readonly Vector3 Point;

    public HitParams(GameObject attacker, ETeam team, int damage, Vector3 direction, float knockback, float hitStop, Vector3 point)
    {
        Attacker = attacker;
        Team = team;
        Damage = damage;
        Direction = direction;
        Knockback = knockback;
        HitStop = hitStop;
        Point = point;
    }
}

// 피격 반응 하나. 체력, 넉백, 번쩍, 히트스톱처럼 반응마다 클래스를 나눠 조합한다.
// 실행 순서는 HitFeature에 넣은 순서(UnitController.Awake)
public interface IHitReaction
{
    void OnHit(in HitParams hit);
}

// 맞을 수 있는 대상. 근접 공격이 맞힌 콜라이더에서 이것을 찾는다
public interface IHitTarget
{
    Transform transform { get; }
    HitFeature HitFeature { get; }
}
