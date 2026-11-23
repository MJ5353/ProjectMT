using UnityEngine;

// ==================================================

// 피해 계산 한곳. 순서: 때린 쪽(공격력 + 패시브) → 부위(부위 피해·몸 피해 배율) → 맞는 쪽(방어 + 패시브).
// HitParams가 때린 쪽 값을, HitFeature가 부위·맞는 쪽 값을 여기서 받는다. 수치는 StatFeature, 조건이 붙는 순간 수치는 PassiveFeature(Attack·Taken 훅).
// 0 이하 피해(갑옷 부위로 막힘 등)는 보정 없이 그대로 둔다

public static class DamageCalculator
{
    private const float MinTakenRate = 0.2f; // 방어를 아무리 올려도 받는 피해는 이 비율 밑으로 내려가지 않는다

    // 때린 쪽 ---

    // 공격 하나의 피해. 공격력 Rate에 패시브 순간 공격력 Rate를 더한다
    public static int GetDamage(UnitController attacker, AttackSpec spec)
    {
        return Scale(spec.Damage, GetAttackRate(attacker, spec, EStatType.Attack));
    }

    // 버섯 부위에 들어가는 피해
    public static int GetPartDamage(UnitController attacker, int damage)
    {
        return Scale(damage, attacker.StatF.GetRate(EStatType.PartDamage));
    }

    // 판정 반지름
    public static float GetRadius(UnitController attacker, AttackSpec spec)
    {
        return spec.Radius * GetAttackRate(attacker, spec, EStatType.AttackRadius);
    }

    // 맞는 쪽 ---

    // 부위를 맞았으면 몸 피해 배율(bodyRate)을 먼저 곱하고(0이 되면 막힌 것), 1 - (방어 + 패시브 순간 방어)를 곱한다
    // hit은 Taken 패시브 조건·효과가 읽는다(때린 쪽·공격 등)
    public static int GetTaken(UnitController target, in HitParams hit, float bodyRate = 1f)
    {
        int damage = Mathf.RoundToInt(hit.Damage * bodyRate);
        if (damage <= 0)
            return damage;

        float rate = target.StatF.GetReduceRate(EStatType.Defense, 0f)
                     - target.PassiveF.GetRate(EPassiveTrigger.Taken, PassiveContext.S_Taken(target, hit), EStatType.Defense);

        return Scale(damage, Mathf.Max(MinTakenRate, rate));
    }

    // ==================================================

    private static float GetAttackRate(UnitController attacker, AttackSpec spec, EStatType stat)
    {
        return attacker.StatF.GetRate(stat) + attacker.PassiveF.GetRate(EPassiveTrigger.Attack, PassiveContext.S_Attack(attacker, spec), stat);
    }

    // 0 이하는 그대로, 양수는 최소 1
    private static int Scale(int damage, float rate)
    {
        return damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * rate));
    }
}
