using System.Collections.Generic;
using System.Linq;

// ==================================================

public enum EGearOpenResult
{
    Ok,
    Opened,         // 이미 열림
    Locked,         // 앞 노드가 덜 열림
    NotEnoughGold,
}

// ==================================================

// 정비 탭 강화 노드. 연 노드 ID를 저장하고, 열기 요청·엠블렘 단계·전투 보정값(UnitStat)을 맡는다.
// UI는 TryOpen만 부르고 GearChanged를 받아 다시 그린다. 노드·엠블렘 목록은 DB(GearNodeDef·EmblemDef)에서 처음 쓸 때 모은다

public class GearData : PlayerDataBase
{
    private readonly InventoryData m_Inventory;
    private readonly HashSet<string> m_SetOpened = new();
    private readonly List<GearNodeDef> m_ListNode = new(); // DB의 노드 전부(ID 순)
    private readonly List<EmblemDef> m_ListEmblem = new(); // DB의 엠블렘 전부(NeedCount 오름차순)

    public int OpenedCount => m_SetOpened.Count;
    public int TotalCount => ListNode.Count;

    public IReadOnlyList<GearNodeDef> ListNode
    {
        get
        {
            CollectDefs();
            return m_ListNode;
        }
    }

    // ------------------------------------------------

    public GearData(InventoryData inventory)
    {
        m_Inventory = inventory;
    }

    public override void Clear()
    {
        m_SetOpened.Clear();
    }

    public override void Load(SaveData data)
    {
        foreach (string nodeID in data.gearNodes)
        {
            if (nodeID.E_IsNullOrEmpty() == false)
                m_SetOpened.Add(nodeID);
        }
    }

    public override void Save(SaveData data)
    {
        data.gearNodes.AddRange(m_SetOpened);
    }

    // 조건을 보고 골드를 내고 연다. 열리면 GearChanged
    public EGearOpenResult TryOpen(GearNodeDef node)
    {
        EGearOpenResult result = CheckOpen(node);
        if (result != EGearOpenResult.Ok)
            return result;

        if (m_Inventory.TryRemove(CurrencyDef.GoldID, node.Cost) == false)
            return EGearOpenResult.NotEnoughGold;

        m_SetOpened.Add(node.ID);
        SEventManager.Publish(new GearChanged(node.ID));

        return EGearOpenResult.Ok;
    }

    // 플레이어 보정값을 채운다. 연 노드 → 엠블렘 보너스(지난 단계까지) 순
    public void FillStat(UnitStat stat)
    {
        foreach (GearNodeDef node in ListNode)
        {
            if (IsOpened(node) == false)
                continue;

            stat.Add(node.Stat, node.StatValue);
            AddSpecial(stat, node);
        }

        foreach (EmblemDef emblem in m_ListEmblem)
        {
            if (emblem.NeedCount > OpenedCount)
                break;

            foreach (EmblemDef.Bonus bonus in emblem.ListBonus)
            {
                stat.Add(bonus.Stat, bonus.Value);
            }
        }
    }

    // 지금 단계까지 엠블렘 보너스를 능력치별로 합친다(정비 탭 표시용)
    public void CollectEmblemBonus(Dictionary<EGearStat, float> result)
    {
        CollectDefs();
        result.Clear();

        foreach (EmblemDef emblem in m_ListEmblem)
        {
            if (emblem.NeedCount > OpenedCount)
                break;

            foreach (EmblemDef.Bonus bonus in emblem.ListBonus)
            {
                result.TryGetValue(bonus.Stat, out float sum);
                result[bonus.Stat] = sum + bonus.Value;
            }
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugReset()
    {
        m_SetOpened.Clear();
        SEventManager.Publish(new GearChanged(null));
    }
#endif

    // get ---

    public bool IsOpened(GearNodeDef node) => m_SetOpened.Contains(node.ID);

    // 앞 노드가 모두 열렸는지
    public bool IsReachable(GearNodeDef node)
    {
        foreach (GearNodeDef require in node.ListRequire)
        {
            if (require != null && IsOpened(require) == false)
                return false;
        }

        return true;
    }

    public EGearOpenResult CheckOpen(GearNodeDef node)
    {
        if (IsOpened(node))
            return EGearOpenResult.Opened;

        if (IsReachable(node) == false)
            return EGearOpenResult.Locked;

        if (m_Inventory.Has(CurrencyDef.GoldID, node.Cost) == false)
            return EGearOpenResult.NotEnoughGold;

        return EGearOpenResult.Ok;
    }

    // 지금 단계. 엠블렘이 없으면 null
    public EmblemDef GetEmblem()
    {
        CollectDefs();

        EmblemDef current = null;
        foreach (EmblemDef emblem in m_ListEmblem)
        {
            if (emblem.NeedCount > OpenedCount)
                break;

            current = emblem;
        }

        return current;
    }

    // 다음 단계. 최고 단계면 null
    public EmblemDef GetNextEmblem()
    {
        CollectDefs();
        return m_ListEmblem.FirstOrDefault(emblem => emblem.NeedCount > OpenedCount);
    }

    // ==================================================

    private static void AddSpecial(UnitStat stat, GearNodeDef node)
    {
        switch (node.Special)
        {
            case EGearSpecial.Insurance:
            {
                stat.AddInsurance(node.SpecialRange);
                break;
            }
            case EGearSpecial.SkillBoost:
            {
                if (node.TargetSkill != null)
                    stat.AddSkillBoost(node.TargetSkill.Attack, node.SpecialValue, node.SpecialValue2);
                
                break;
            }
            case EGearSpecial.LastStand:
            {
                stat.SetLastStand(node.SpecialValue, node.SpecialValue2);
                break;
            }
            case EGearSpecial.SporeRegen:
            {
                stat.AddPickupHeal(UnityEngine.Mathf.RoundToInt(node.SpecialValue));
                break;
            }
        }
    }
    
    // DB가 준비된 뒤(처음 쓸 때) 한 번 모은다
    private void CollectDefs()
    {
        if (m_ListNode.Count > 0 || m_ListEmblem.Count > 0)
            return;

        GameDatabase database = GameManager.Instance.Database;
        m_ListNode.AddRange(database.GetAll<GearNodeDef>().OrderBy(node => node.ID));
        m_ListEmblem.AddRange(database.GetAll<EmblemDef>().OrderBy(emblem => emblem.NeedCount));
    }
}
