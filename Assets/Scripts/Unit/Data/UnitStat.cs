using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// [mj] todo) stat 관리구조?

// 유닛 하나의 강화 보정값 묶음. 몬스터는 비어 있고(배율 1), 플레이어는 스폰 직후 BattleService가 정비(GearData)로 채운다.
// 유닛·부품은 PlayerDataManager를 모르고 이 값만 읽는다. 풀에서 다시 꺼낼 때(UnitController.OnSpawn) 비운다

public class UnitStat
{
    private const float MinTakenRate = 0.2f;        // 방어를 아무리 올려도 받는 피해는 이 비율 밑으로 내려가지 않는다
    private const float MinDashCooldownRate = 0.2f; // 대시 쿨 하한 비율

    private static readonly int s_StatCount = Enum.GetValues(typeof(EGearStat)).Length;

    private readonly float[] m_ArrBonus = new float[s_StatCount];                 // EGearStat 순서
    private readonly Dictionary<AttackSpec, Vector2> m_DicSkillBoost = new();     // 공격 → (피해 배율, 범위 배율)

    // 스페셜 ---
    public Vector2Int InsuranceGold { get; private set; }   // 맞으면 받는 골드(최소~최대). 0이면 없음
    public int PickupHeal { get; private set; }             // 버섯을 주울 때 회복하는 체력
    private float m_LastStandRate;                          // 체력이 낮을 때 더 줄이는 받는 피해 비율
    private float m_LastStandHpRatio;                       // 이 체력 비율 이하일 때

    // get ---
    public float AttackSpeedRate => 1f + Get(EGearStat.AttackSpeed);
    public float MoveSpeedRate => 1f + Get(EGearStat.MoveSpeed);
    public float MaxHpRate => 1f + Get(EGearStat.MaxHp);
    public float DashTimeRate => 1f + Get(EGearStat.DashDistance);
    public float DashCooldownRate => Mathf.Max(MinDashCooldownRate, 1f - Get(EGearStat.Dodge));
    public float BreakResist => Get(EGearStat.BreakResist);

    // ------------------------------------------------

    public void Clear()
    {
        Array.Clear(m_ArrBonus, 0, m_ArrBonus.Length);
        m_DicSkillBoost.Clear();
        InsuranceGold = Vector2Int.zero;
        PickupHeal = 0;
        m_LastStandRate = 0f;
        m_LastStandHpRatio = 0f;
    }

    public void Add(EGearStat stat, float value)
    {
        if (stat == EGearStat.None)
            return;

        m_ArrBonus[(int)stat] += value;
    }

    public float Get(EGearStat stat) => m_ArrBonus[(int)stat];

    // 같은 공격을 여러 노드가 올리면 곱한다
    public void AddSkillBoost(AttackSpec spec, float damageRate, float radiusRate)
    {
        if (spec == null)
            return;

        Vector2 boost = m_DicSkillBoost.TryGetValue(spec, out Vector2 before) ? before : Vector2.one;
        m_DicSkillBoost[spec] = new Vector2(boost.x * Mathf.Max(0f, damageRate), boost.y * Mathf.Max(0f, radiusRate));
    }

    public void AddInsurance(Vector2Int goldRange) => InsuranceGold += goldRange;

    public void AddPickupHeal(int amount) => PickupHeal += amount;

    public void SetLastStand(float rate, float hpRatio)
    {
        m_LastStandRate = Mathf.Max(m_LastStandRate, rate);
        m_LastStandHpRatio = Mathf.Max(m_LastStandHpRatio, hpRatio);
    }

    // 전투 계산 ---

    // 공격 하나의 피해. 공격력 보정과 공격 강화를 곱한다
    public int GetDamage(AttackSpec spec)
    {
        float rate = (1f + Get(EGearStat.Attack)) * GetBoost(spec).x;
        return spec.Damage <= 0 ? spec.Damage : Mathf.Max(1, Mathf.RoundToInt(spec.Damage * rate));
    }

    // 버섯 부위에 들어가는 피해
    public int GetPartDamage(int damage)
    {
        return damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * (1f + Get(EGearStat.PartDamage))));
    }

    public float GetRadius(AttackSpec spec) => spec.Radius * GetBoost(spec).y;

    // 받는 피해. 방어와 든든한 뿌리(체력이 낮을 때)를 곱한다. 0 이하(갑옷 부위로 막힌 피해)는 그대로
    public int ReduceTaken(int damage, float hpRatio)
    {
        if (damage <= 0)
            return damage;

        float rate = 1f - Get(EGearStat.Defense);
        if (m_LastStandRate > 0f && hpRatio <= m_LastStandHpRatio)
            rate *= 1f - m_LastStandRate;

        return Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Max(MinTakenRate, rate)));
    }

    private Vector2 GetBoost(AttackSpec spec)
    {
        return spec != null && m_DicSkillBoost.TryGetValue(spec, out Vector2 boost) ? boost : Vector2.one;
    }
}
