using UnityEngine;

// ==================================================

// 피해 계산 한곳. 순서: 때린 쪽(공격력·공격 강화) → 부위(부위 피해·몸 피해 배율) → 맞는 쪽(방어·든든한 뿌리).
// HitParams가 때린 쪽 값을, HitFeature가 부위·맞는 쪽 값을 여기서 받는다. 수치 보정은 모두 StatFeature에서 읽는다.
// 0 이하 피해(갑옷 부위로 막힘 등)는 보정 없이 그대로 둔다

public static class DamageCalculator
{
    private const float MinTakenRate = 0.2f; // 방어를 아무리 올려도 받는 피해는 이 비율 밑으로 내려가지 않는다

    // 때린 쪽 ---

    // 공격 하나의 피해. 공격력과 공격 강화를 곱한다
    public static int GetDamage(UnitController attacker, AttackSpec spec)
    {
        StatFeature stat = attacker.StatF;
        return Scale(spec.Damage, stat.GetRate(EStatType.Attack) * stat.GetSkillBoost(spec).x);
    }

    // 버섯 부위에 들어가는 피해
    public static int GetPartDamage(UnitController attacker, int damage)
    {
        return Scale(damage, attacker.StatF.GetRate(EStatType.PartDamage));
    }

    // 판정 반지름(공격 강화)
    public static float GetRadius(UnitController attacker, AttackSpec spec)
    {
        return spec.Radius * attacker.StatF.GetSkillBoost(spec).y;
    }

    // 맞는 쪽 ---

    // 부위를 맞았으면 몸 피해 배율(bodyRate)을 먼저 곱하고(0이 되면 막힌 것), 방어·든든한 뿌리를 곱한다
    public static int GetTaken(UnitController target, int damage, float bodyRate = 1f)
    {
        damage = Mathf.RoundToInt(damage * bodyRate);
        if (damage <= 0)
            return damage;

        StatFeature stat = target.StatF;
        float rate = stat.GetReduceRate(EStatType.Defense, MinTakenRate);
        if (stat.LastStandRate > 0f && target.HealthF.Ratio <= stat.LastStandHpRatio)
            rate *= 1f - stat.LastStandRate;

        return Scale(damage, Mathf.Max(MinTakenRate, rate));
    }

    // ==================================================

    // 0 이하는 그대로, 양수는 최소 1
    private static int Scale(int damage, float rate)
    {
        return damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * rate));
    }
}
