using UnityEngine;

public enum ETeam
{
    Player,
    Mob,
}

// 한 번의 타격 정보. 공격하는 쪽이 만들고, 맞는 쪽 HitFeature가 반응 컴포넌트들에 그대로 넘긴다.
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

// 피격 반응 하나. 체력, 넉백, 번쩍, 히트스톱처럼 반응마다 컴포넌트를 나눠 조합한다.
// Order가 작은 것부터 실행된다(체력이 먼저라서 뒤 반응은 IsDead를 볼 수 있다).
public interface IHitReaction
{
    int Order { get; }
    void OnHit(in HitParams hit);
}
