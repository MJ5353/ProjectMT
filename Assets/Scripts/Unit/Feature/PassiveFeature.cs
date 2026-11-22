using System.Collections.Generic;

// ==================================================

// 유닛의 패시브(PassiveDef) 목록. 출처(정비 노드, 나중에 장비 등)별로 넣고 빼며, 훅 지점에서 차례로 부른다.
// 부르는 곳: 공격·받는 피해(DamageCalculator), 맞은 뒤(UnitController.OnHit), 주웠을 때(BattleService). 스페셜 이름은 여기도 부르는 쪽도 모른다.
// 풀에서 다시 꺼낼 때(Reset) 모두 비운다

public class PassiveFeature : UnitFeature
{
    private readonly Dictionary<object, PassiveDef> m_DicSource = new();
    private readonly List<PassiveDef> m_ListPassive = new(); // 훅을 돌 목록. 출처가 바뀔 때만 다시 채운다

    // ------------------------------------------------

    public PassiveFeature(UnitController owner) : base(owner) { }

    public override void Reset()
    {
        ClearAll();
    }

    public override void Clear()
    {
        ClearAll();
    }

    // 출처 ---

    // 같은 출처를 다시 넣으면 앞 것을 바꾼다
    public void AddSource(object source, PassiveDef passive)
    {
        if (source == null || passive == null)
            return;

        m_DicSource[source] = passive;
        Rebuild();
    }

    public void RemoveSource(object source)
    {
        if (source == null || m_DicSource.Remove(source) == false)
            return;

        Rebuild();
    }

    // 훅 ---

    // 피해 계산 중(Attack·Taken) 순간 능력치 Rate 합
    public float GetRate(EPassiveTrigger trigger, in PassiveContext context, EStatType stat)
    {
        float rate = 0f;
        foreach (PassiveDef passive in m_ListPassive)
        {
            rate += passive.GetRate(trigger, context, stat);
        }

        return rate;
    }

    public void OnHit(in HitParams hit)
    {
        Execute(EPassiveTrigger.Hit, PassiveContext.S_Hit(Owner, hit));
    }

    public void OnPickup(in LootPicked loot)
    {
        Execute(EPassiveTrigger.Pickup, PassiveContext.S_Pickup(Owner, loot));
    }

    // ==================================================

    private void Execute(EPassiveTrigger trigger, in PassiveContext context)
    {
        foreach (PassiveDef passive in m_ListPassive)
        {
            passive.Execute(trigger, context);
        }
    }

    private void ClearAll()
    {
        m_DicSource.Clear();
        m_ListPassive.Clear();
    }

    private void Rebuild()
    {
        m_ListPassive.Clear();
        m_ListPassive.AddRange(m_DicSource.Values);
    }
}
