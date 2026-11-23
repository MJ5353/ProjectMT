using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 유닛 능력치. 출처(정비 노드·엠블렘, 나중에 장비·코스튬·버프)별 수정치(StatModifier)를 모아 최종값을 낸다.
// 최종값 = 기본값 × (1 + Rate 합) + Add 합. 기본값은 쓰는 쪽이 Def에서 넘긴다(StatFeature는 Def 수치를 모른다).
// 출처는 AddSource·RemoveSource로 넣고 빼고, 바뀌면 Changed(최대 체력 등이 다시 계산한다). 풀에서 다시 꺼낼 때(Reset) 출처를 모두 비운다.
// 몬스터는 출처가 없어 기본값 그대로, 플레이어는 스폰 직후 BattleService가 정비(GearData)를 넣는다.
// 다른 부품이 Changed를 생성자에서 구독하므로 UnitController가 맨 먼저 만든다. 수치 하나로 안 되는 효과는 PassiveFeature

public class StatFeature : UnitFeature
{
    private static readonly int s_StatCount = Enum.GetValues(typeof(EStatType)).Length;

    private readonly Dictionary<object, List<StatModifier>> m_DicSource = new();   // 출처 → 수정치
    private readonly Stack<List<StatModifier>> m_StackFreeList = new();            // 빠진 출처의 리스트. 스폰마다 new 하지 않게 다시 쓴다
    private readonly float[] m_ArrRate = new float[s_StatCount];                    // EStatType 순서. Rate 합
    private readonly float[] m_ArrAdd = new float[s_StatCount];                     // Add 합

    public event Action Changed;

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
        foreach (List<StatModifier> list in m_DicSource.Values)
        {
            m_StackFreeList.Push(list);
        }

        m_DicSource.Clear();
        
        Array.Clear(m_ArrRate, 0, m_ArrRate.Length);
        Array.Clear(m_ArrAdd, 0, m_ArrAdd.Length);
    }

    // 출처 ---

    // 같은 출처를 다시 넣으면 앞 것을 바꾼다
    public void AddSource(object source, IReadOnlyList<StatModifier> listModifier)
    {
        if (source == null || listModifier == null || listModifier.Count == 0)
            return;

        if (m_DicSource.TryGetValue(source, out List<StatModifier> list) == false)
        {
            list = m_StackFreeList.Count > 0 ? m_StackFreeList.Pop() : new List<StatModifier>();
            m_DicSource[source] = list;
        }

        list.Clear();
        list.AddRange(listModifier);
        
        Recalculate();
    }

    public void RemoveSource(object source)
    {
        if (source == null || m_DicSource.Remove(source, out List<StatModifier> list) == false)
            return;

        m_StackFreeList.Push(list);
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
}
