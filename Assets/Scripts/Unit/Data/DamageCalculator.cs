using UnityEngine;

// ==================================================

// 피해 계산 한곳. 순서: 때린 쪽(공격력 → 패시브) → 부위(부위 피해·몸 피해 배율) → 맞는 쪽(방어 → 패시브).
// HitParams가 때린 쪽 값을, HitFeature가 부위·맞는 쪽 값을 여기서 받는다. 수치는 StatFeature, 수치 하나로 안 되는 효과는 PassiveFeature 훅.
// 0 이하 피해(갑옷 부위로 막힘 등)는 보정 없이 그대로 둔다

public static class DamageCalculator
{
    private const float MinTakenRate = 0.2f; // 방어를 아무리 올려도 받는 피해는 이 비율 밑으로 내려가지 않는다

    // 때린 쪽 ---

    // 공격 하나의 피해. 공격력과 패시브(공격 강화 등)를 곱한다
    public static int GetDamage(UnitController attacker, AttackSpec spec)
    {
        float damageRate = attacker.StatF.GetRate(EStatType.Attack);
        float radiusRate = 1f;
        attacker.PassiveF.ModifyAttack(spec, ref damageRate, ref radiusRate);

        return Scale(spec.Damage, damageRate);
    }

    // 버섯 부위에 들어가는 피해
    public static int GetPartDamage(UnitController attacker, int damage)
    {
        return Scale(damage, attacker.StatF.GetRate(EStatType.PartDamage));
    }

    // 판정 반지름(패시브)
    public static float GetRadius(UnitController attacker, AttackSpec spec)
    {
        float damageRate = 1f;
        float radiusRate = 1f;
        attacker.PassiveF.ModifyAttack(spec, ref damageRate, ref radiusRate);

        return spec.Radius * radiusRate;
    }

    // 맞는 쪽 ---

    // 부위를 맞았으면 몸 피해 배율(bodyRate)을 먼저 곱하고(0이 되면 막힌 것), 방어와 패시브(든든한 뿌리 등)를 곱한다
    public static int GetTaken(UnitController target, int damage, float bodyRate = 1f)
    {
        damage = Mathf.RoundToInt(damage * bodyRate);
        if (damage <= 0)
            return damage;

        float rate = target.StatF.GetReduceRate(EStatType.Defense, MinTakenRate);
        target.PassiveF.ModifyTaken(ref rate);

        return Scale(damage, Mathf.Max(MinTakenRate, rate));
    }

    // ==================================================

    // 0 이하는 그대로, 양수는 최소 1
    private static int Scale(int damage, float rate)
    {
        return damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * rate));
    }
}
