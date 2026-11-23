using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 몬스터 공격 고르기(AI 서브, 유닛 수명). 분노하면 후보가 늘고 공용 쿨이 준다(Enrage). 후보(Def의 공격 전부)마다 개별 쿨을, 몬스터 전체에 공용 쿨을 돌리고
// 거리·가중치로 이번 공격(Current)을 고른다. 언제 고를지는 추적 코어(ChaseCore)가 정한다.
// 공격이 끝날 때마다 쿨을 걸고, 끝까지 했으면 콤보 다음 타(StateFeature.ComboNext)를 예약한다(그 프레임 요청 정리에서 바로 나간다)

public class AttackPickerSub : SubState
{
    private readonly TargetSenseSub m_Sense;                  // 콤보 다음 타도 노리는 적 쪽으로 돈다

    private readonly List<AttackSpec> m_ListChoice = new();   // 고를 수 있는 공격: Def.ListSkill
    private readonly List<float> m_ListCooldown = new();      // m_ListChoice 칸마다 남은 개별 쿨
    private readonly float m_SharedCooldown;                  // 공격이 끝날 때 거는 공용 쿨(AIDef.AttackCooldown)
    private readonly int m_BaseCount;                         // Def 공격 수. 그 뒤는 분노 때 더한 공격
    private float m_CooldownRate = 1f;                        // 공용 쿨 배율. 분노하면 줄인다

    public override ESubLifetime Lifetime => ESubLifetime.Unit;
    public AttackSpec Current { get; private set; }           // 추적에서 고른 공격(콤보면 첫 타). 쿨은 이 공격에 건다
    public float CooldownRemain { get; private set; }         // 공용 쿨

    // ------------------------------------------------

    public AttackPickerSub(UnitController owner, UnitDef def, float sharedCooldown, TargetSenseSub sense) : base(owner)
    {
        m_Sense = sense;
        m_SharedCooldown = sharedCooldown;
        def.CollectAttacks(m_ListChoice);
        
        m_BaseCount = m_ListChoice.Count;
        for (int i = 0; i < m_ListChoice.Count; i++)
        {
            m_ListCooldown.Add(0f);
        }
    }

    // 꺼낼 때·다시 켤 때(다시 켜는 건 스폰 직후 시작 팝업뿐이라 스폰 때와 같다)
    protected override void Enter()
    {
        // 분노 때 더한 공격을 걷어낸다
        m_ListChoice.RemoveRange(m_BaseCount, m_ListChoice.Count - m_BaseCount);
        m_ListCooldown.RemoveRange(m_BaseCount, m_ListCooldown.Count - m_BaseCount);
        m_CooldownRate = 1f;

        Current = null;
        CooldownRemain = 0f;
        for (int i = 0; i < m_ListCooldown.Count; i++)
        {
            m_ListCooldown[i] = 0f;
        }
    }

    protected override void Tick(float deltaTime)
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

    // 분노(BossWatchSub). 공격 후보를 더하고 공용 쿨을 줄인다. 풀에서 다시 꺼내면(Reset) 원래대로
    public void Enrage(IReadOnlyList<SkillDef> listSkill, float cooldownRate)
    {
        m_CooldownRate = cooldownRate;
        for (int i = 0; i < listSkill.Count; ++i)
        {
            SkillDef skill = listSkill[i];
            if (skill == null)
                continue;

            m_ListChoice.Add(skill.Attack);
            m_ListCooldown.Add(0f);
        }
    }

    // 공격이 끝날 때(끊겨도) 쿨. 끝까지 했으면 콤보 다음 타를 대상 쪽으로 예약한다. 보스 감시는 예약이 있으면 행동 중으로 본다
    public override void OnCoreChanged(ECoreState prev, ECoreState next, bool isFinished)
    {
        if (prev != ECoreState.Attack)
            return;

        StartCooldown();

        AttackSpec comboNext = Owner.StateF.ComboNext;
        if (isFinished && comboNext != null)
            Owner.StateF.ReserveAttack(comboNext, Vector3.zero, m_Sense.Selector.TargetTransform);
    }

    // 공용 쿨과 쓴 공격의 개별 쿨을 건다
    private void StartCooldown()
    {
        CooldownRemain = m_SharedCooldown * m_CooldownRate;

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
