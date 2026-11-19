using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 몬스터 공격 고르기. 후보(Def의 공격 전부)마다 개별 쿨을, 몬스터 전체에 공용 쿨을 돌리고
// 거리·가중치로 이번 공격(Current)을 고른다. 콤보(AttackSpec.Next)면 다음 타로 넘어간다.
// 언제 고를지는 상태(추적·공격)가 정하고, 여기는 고르기와 쿨만 한다

public class AttackChoiceFeature : UnitFeature
{
    private readonly List<AttackSpec> m_ListChoice = new();   // 고를 수 있는 공격: Def.ListSkill
    private readonly List<float> m_ListCooldown = new();      // m_ListChoice 칸마다 남은 개별 쿨
    private readonly float m_SharedCooldown;                  // 공격이 끝날 때 거는 공용 쿨(MonsterDef.AttackCooldown)

    public AttackSpec Current { get; private set; }           // 추적에서 고른, 이번에 쓸 공격
    public float CooldownRemain { get; private set; }         // 공용 쿨

    // ------------------------------------------------

    public AttackChoiceFeature(UnitController owner, UnitDef def, float sharedCooldown) : base(owner)
    {
        m_SharedCooldown = sharedCooldown;
        def.CollectAttacks(m_ListChoice);
        for (int i = 0; i < m_ListChoice.Count; i++)
        {
            m_ListCooldown.Add(0f);
        }
    }

    public override void Reset()
    {
        Current = null;
        CooldownRemain = 0f;
        for (int i = 0; i < m_ListCooldown.Count; i++)
        {
            m_ListCooldown[i] = 0f;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (CooldownRemain > 0f)
            CooldownRemain -= deltaTime;

        for (int i = 0; i < m_ListCooldown.Count; i++)
        {
            if (m_ListCooldown[i] > 0f)
                m_ListCooldown[i] -= deltaTime;
        }
    }

    // 공용 쿨이 돌았으면 개별 쿨이 돌고 거리가 맞는 공격 중 가중치로 하나 골라 Current에 둔다
    public bool TryPick(float distance)
    {
        if (CooldownRemain > 0f)
            return false;

        float totalWeight = 0f;
        for (int i = 0; i < m_ListChoice.Count; i++)
        {
            if (CanUse(i, distance))
                totalWeight += m_ListChoice[i].Weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        for (int i = 0; i < m_ListChoice.Count; i++)
        {
            if (CanUse(i, distance) == false)
                continue;

            Current = m_ListChoice[i];

            roll -= Current.Weight;
            if (roll <= 0f)
                break;
        }

        return true;
    }

    // 콤보 다음 타가 있으면 Current를 넘기고 true
    public bool TryContinueCombo()
    {
        AttackSpec next = Current?.NextAttack;
        if (next == null)
            return false;

        Current = next;
        return true;
    }

    // 공격이 끝날 때. 공용 쿨과 쓴 공격의 개별 쿨을 건다
    public void StartCooldown()
    {
        CooldownRemain = m_SharedCooldown;

        int index = m_ListChoice.IndexOf(Current);
        if (index >= 0)
            m_ListCooldown[index] = Current.Cooldown;
    }

    // get --- 
    
    private bool CanUse(int index, float distance)
    {
        AttackSpec attack = m_ListChoice[index];
        if (attack.Weight <= 0f || m_ListCooldown[index] > 0f)
            return false;

        return distance >= attack.MinRange && distance <= attack.MaxRange;
    }
}
