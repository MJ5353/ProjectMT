using UnityEngine;

// ==================================================

public enum ETeam
{
    Player,
    Monster,
}

// ==================================================

// 한 번의 타격 정보. 공격하는 쪽이 만들고, 맞는 쪽 HitFeature가 체력·반응 처리에 쓴다.

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

// ==================================================

// 액터 검색과 근접 공격에 필요한 피격 정보.

public interface IHitTarget
{
    Transform transform { get; }
    HitFeature HitFeature { get; }
    CharacterController HurtBody { get; }
}
