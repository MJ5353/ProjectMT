using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==================================================

// 맵 스폰 자동 배정. 맵 FBX의 SpawnSlot_NNN을 읽어 MapDef 스폰 목록을 채운다.
// 슬롯 배치는 tools/voxel/SpawnSlots.md: 플레이어 → 잡몹(A A B 반복) → 엘리트(A B 반복, 4칸) → 보스(마지막 1칸)
// 몬스터는 번호로 고른다. 지역 순서(Order) n이면 (n-1)*5+1 ~ +5번: 잡몹 A, 잡몹 B, 엘리트 A, 엘리트 B, 보스
// 이미 있던 슬롯의 리스폰 시간은 그대로 두고, 새 슬롯만 등급별 기본값을 쓴다

public static class MapSpawnAutoFill
{
    public readonly struct SlotPick
    {
        public readonly int Slot;
        public readonly MonsterDef Monster;
        public readonly float RespawnDelay;

        public SlotPick(int slot, MonsterDef monster, float respawnDelay)
        {
            Slot = slot;
            Monster = monster;
            RespawnDelay = respawnDelay;
        }
    }

    // 맵 하나에 넣을 값
    public class Plan
    {
        public MapDef Map { get; }
        public List<SlotPick> ListPick { get; } = new();
        public List<string> ListWarning { get; } = new();

        public Plan(MapDef map)
        {
            Map = map;
        }
    }

    private const int EliteSlotCount = 4;
    private const int NormalGroupSize = 3;      // 잡몹 슬롯은 A A B 순서로 반복
    private const float NormalRespawn = 4f;     // 기존 Map_001 값
    private const float EliteRespawn = 8f;
    private const float BossRespawn = -1f;      // 보스는 다시 안 나옴

    // ------------------------------------------------

    public static bool Run(IReadOnlyList<MapDef> listMap)
    {
        if (listMap == null || listMap.Count == 0)
            return false;

        List<Plan> listPlan = MakePlans(listMap);
        string report = Describe(listPlan);
        CLogger.Log("MapSpawnAutoFill", report);

        if (EditorUtility.DisplayDialog("맵 스폰 자동 배정", report + "\nCtrl+Z로 되돌릴 수 있습니다.", "적용", "취소") == false)
            return false;

        Apply(listPlan);
        return true;
    }

    // 계획 ---

    public static List<Plan> MakePlans(IReadOnlyList<MapDef> listMap)
    {
        List<MonsterDef> listMonster = DataTools.CollectDefs().OfType<MonsterDef>().ToList();
        List<Plan> listPlan = new();

        foreach (MapDef map in listMap.OrderBy(map => map.Order))
        {
            Plan plan = new Plan(map);
            FillPlan(plan, listMonster);
            listPlan.Add(plan);
        }

        return listPlan;
    }

    private static void FillPlan(Plan plan, List<MonsterDef> listMonster)
    {
        MapDef map = plan.Map;
        GameObject model = BattleSetup.LoadModel(map.name);
        if (model == null)
        {
            plan.ListWarning.Add($"{map.name}.fbx 없음. 건너뜀");
            return;
        }

        List<int> listSlot = new();
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
        {
            if (MapDef.TryParseSlot(t.name, out int slot) && slot != map.PlayerSlot && listSlot.Contains(slot) == false)
                listSlot.Add(slot);
        }
        listSlot.Sort();
        if (listSlot.Count == 0)
        {
            plan.ListWarning.Add("SpawnSlot이 없음");
            return;
        }

        // 지역 몬스터 5종: [0] 잡몹 A, [1] 잡몹 B, [2] 엘리트 A, [3] 엘리트 B, [4] 보스
        int firstNo = (map.Order - 1) * MonsterFormula.MonstersPerMap + 1;
        MonsterDef[] arrMonster = new MonsterDef[MonsterFormula.MonstersPerMap];
        foreach (MonsterDef monster in listMonster)
        {
            if (MonsterAutoFill.TryParseNo(monster.name, out int no) == false)
                continue;

            int index = no - firstNo;
            if (index >= 0 && index < arrMonster.Length)
                arrMonster[index] = monster;
        }

        int bossIndex = listSlot.Count - 1;
        int eliteStart = Mathf.Max(0, bossIndex - EliteSlotCount);
        for (int i = 0; i < listSlot.Count; i++)
        {
            int kind;
            float respawn;
            if (i == bossIndex)
            {
                kind = 4;
                respawn = BossRespawn;
            }
            else if (i >= eliteStart)
            {
                kind = 2 + (i - eliteStart) % 2;
                respawn = EliteRespawn;
            }
            else
            {
                kind = i % NormalGroupSize == NormalGroupSize - 1 ? 1 : 0;
                respawn = NormalRespawn;
            }

            MonsterDef monster = arrMonster[kind];
            if (monster == null)
            {
                plan.ListWarning.Add($"{MapDef.GetSlotName(listSlot[i])}: {firstNo + kind:000}번 몬스터 Def 없음. 비움");
                continue;
            }

            MapDef.SpawnEntry old = map.ListSpawn.FirstOrDefault(entry => entry != null && entry.Slot == listSlot[i]);
            if (old != null)
                respawn = old.RespawnDelay;

            plan.ListPick.Add(new SlotPick(listSlot[i], monster, respawn));
        }
    }

    // 적용 ---

    private static void Apply(List<Plan> listPlan)
    {
        Undo.SetCurrentGroupName("맵 스폰 자동 배정");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (Plan plan in listPlan)
        {
            if (plan.ListPick.Count == 0)
                continue;

            Undo.RecordObject(plan.Map, "맵 스폰 자동 배정");
            plan.Map.EditorSetSpawns(plan.ListPick.Select(pick => new MapDef.SpawnEntry(pick.Slot, pick.Monster, pick.RespawnDelay)).ToList());
            EditorUtility.SetDirty(plan.Map);
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
    }

    // 미리 보기 ---

    public static string Describe(List<Plan> listPlan)
    {
        StringBuilder builder = new StringBuilder();
        foreach (Plan plan in listPlan)
        {
            builder.AppendLine($"[{plan.Map.name}] 슬롯 {plan.ListPick.Count}개");

            // 같은 몬스터끼리 묶어 슬롯 번호만 나열
            foreach (var group in plan.ListPick.GroupBy(pick => pick.Monster))
            {
                string slots = string.Join(",", group.Select(pick => pick.Slot));
                builder.AppendLine($"  {group.Key.name}: {slots} (리스폰 {group.First().RespawnDelay})");
            }

            foreach (string warning in plan.ListWarning)
            {
                builder.AppendLine($"  !! {warning}");
            }
        }

        return builder.ToString();
    }
}
