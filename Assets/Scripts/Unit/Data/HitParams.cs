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
    public readonly UnitController Attacker; // 때린 유닛. 맞은 쪽 판단이 노릴 대상으로 쓴다
    public readonly ETeam Team;
    public readonly int Damage;
    
    public readonly Vector3 Direction; // 수평, 정규화
    public readonly float Knockback;
    public readonly float HitStop;
    public readonly Vector3 Point;
    public readonly int PartIndex; // 맞은 부위 번호. -1이면 몸
    public readonly float Break; // 경직 가중치(AttackSpec.Break)
    public readonly float Shake; // 카메라 흔들림 세기(AttackSpec.Shake)
    
    // 공격 수치는 AttackSpec에서 채운다. AttackSpec 없는 피해(함정 등)가 생기면 생성자를 따로 둔다
    public HitParams(UnitController attacker, ETeam team, AttackSpec spec, Vector3 direction, Vector3 point, int partIndex = -1)
    {
        Attacker = attacker;
        Team = team;
        Damage = spec.Damage;

        Direction = direction;
        Knockback = spec.Knockback;
        HitStop = spec.HitStop;
        Point = point;
        PartIndex = partIndex;
        Break = spec.Break;
        Shake = spec.Shake;    }
}
