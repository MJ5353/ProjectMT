using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 유닛 능력치. 출처(정비 노드·엠블렘, 나중에 장비·코스튬·버프)별 수정치(StatModifier)를 모아 최종값을 낸다.
// 최종값 = 기본값 × (1 + Rate 합) + Add 합. 기본값은 쓰는 쪽이 Def에서 넘긴다(StatFeature는 Def 수치를 모른다).
// 출처는 AddSource·RemoveSource로 넣고 빼고, 바뀌면 Changed(최대 체력 등이 다시 계산한다). 풀에서 다시 꺼낼 때(Reset) 출처를 모두 비운다.
// 몬스터는 출처가 없어 기본값 그대로, 플레이어는 스폰 직후 BattleService가 정비(GearData)를 넣는다.
// 다른 부품이 Changed를 생성자에서 구독하므로 UnitController가 맨 먼저 만든다

public class StatFeature : UnitFeature
{
    private static readonly int s_StatCount = Enum.GetValues(typeof(EStatType)).Length;

    private readonly Dictionary<object, List<StatModifier>> m_DicSource = new();   // 출처 → 수정치
    private readonly float[] m_ArrRate = new float[s_StatCount];                    // EStatType 순서. Rate 합
    private readonly float[] m_ArrAdd = new float[s_StatCount];                     // Add 합

    public event Action Changed;

    // 스페셜(수치 하나로 안 되는 정비 효과) ---
    // 훅 구조(PassiveDef)로 옮기기 전까지 여기에 둔다. 출처를 비울 때 같이 비운다
    private readonly Dictionary<AttackSpec, Vector2> m_DicSkillBoost = new();     // 공격 → (피해 배율, 범위 배율)
    public Vector2Int InsuranceGold { get; private set; }   // 맞으면 받는 골드(최소~최대). 0이면 없음
    public int PickupHeal { get; private set; }             // 버섯을 주울 때 회복하는 체력
    public float LastStandRate { get; private set; }        // 체력이 낮을 때 더 줄이는 받는 피해 비율
    public float LastStandHpRatio { get; private set; }     // 이 체력 비율 이하일 때

    // ------------------------------------------------

    public StatFeature(UnitController owner) : base(owner) { }

    public override void Reset()
    {
        ClearAll();
    }

    public override void Clear()
    {
        ClearAll();
    }
    
    // 다시 꺼낼 때·풀로 돌아갈 때. 체력은 자기 Reset에서 다시 계산하므로 Changed는 보내지 않는다
    private void ClearAll()
    {
        m_DicSource.Clear();
        
        Array.Clear(m_ArrRate, 0, m_ArrRate.Length);
        Array.Clear(m_ArrAdd, 0, m_ArrAdd.Length);

        m_DicSkillBoost.Clear();
        InsuranceGold = Vector2Int.zero;
        PickupHeal = 0;
        LastStandRate = 0f;
        LastStandHpRatio = 0f;
    }

    // 출처 ---

    // 같은 출처를 다시 넣으면 앞 것을 바꾼다
    public void AddSource(object source, IReadOnlyList<StatModifier> listModifier)
    {
        if (source == null || listModifier == null || listModifier.Count == 0)
            return;

        if (m_DicSource.TryGetValue(source, out List<StatModifier> list) == false)
        {
            list = new List<StatModifier>();
            m_DicSource[source] = list;
        }

        list.Clear();
        list.AddRange(listModifier);
        
        Recalculate();
    }

    public void RemoveSource(object source)
    {
        if (source == null || m_DicSource.Remove(source) == false)
            return;

        Recalculate();
    }
    
    private void Recalculate()
    {
        Array.Clear(m_ArrRate, 0, m_ArrRate.Length);
        Array.Clear(m_ArrAdd, 0, m_ArrAdd.Length);

        foreach (List<StatModifier> list in m_DicSource.Values)
        {
            foreach (StatModifier modifier in list)
            {
                if (modifier == null || modifier.Stat == EStatType.None)
                    continue;

                if (modifier.Type == EStatModType.Rate)
                    m_ArrRate[(int)modifier.Stat] += modifier.Value;
                else
                    m_ArrAdd[(int)modifier.Stat] += modifier.Value;
            }
        }

        Changed?.Invoke();
    }
    
    // 값 ---

    // 기본값에 수정치를 반영한 최종값
    public float Get(EStatType stat, float baseValue)
    {
        int index = (int)stat;
        return baseValue * (1f + m_ArrRate[index]) + m_ArrAdd[index];
    }

    // 기본값이 없는 배율 능력치(공격·공격 속도·부위 피해)
    public float GetRate(EStatType stat) => Get(stat, 1f);

    // 줄이는 능력치(방어·회피). 1 - Rate 합, min 밑으로는 내려가지 않는다
    public float GetReduceRate(EStatType stat, float min) => Mathf.Max(min, 1f - m_ArrRate[(int)stat]);

    // 스페셜 ---

    // 같은 공격을 여러 번 올리면 곱한다
    public void AddSkillBoost(AttackSpec spec, float damageRate, float radiusRate)
    {
        if (spec == null)
            return;

        Vector2 boost = GetSkillBoost(spec);
        m_DicSkillBoost[spec] = new Vector2(boost.x * Mathf.Max(0f, damageRate), boost.y * Mathf.Max(0f, radiusRate));
    }

    public Vector2 GetSkillBoost(AttackSpec spec)
    {
        return spec != null && m_DicSkillBoost.TryGetValue(spec, out Vector2 boost) ? boost : Vector2.one;
    }

    public void AddInsurance(Vector2Int goldRange) => InsuranceGold += goldRange;

    public void AddPickupHeal(int amount) => PickupHeal += amount;

    public void SetLastStand(float rate, float hpRatio)
    {
        LastStandRate = Mathf.Max(LastStandRate, rate);
        LastStandHpRatio = Mathf.Max(LastStandHpRatio, hpRatio);
    }

}
