using System.Collections.Generic;
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
    public readonly int PartIndex; // 맞은 부위 번호. -1이면 몸

    public HitParams(GameObject attacker, ETeam team, int damage, Vector3 direction, float knockback, float hitStop, Vector3 point, int partIndex = -1)
    {
        Attacker = attacker;
        Team = team;
        Damage = damage;
        
        Direction = direction;
        Knockback = knockback;
        HitStop = hitStop;
        Point = point;
        PartIndex = partIndex;
    }
}
