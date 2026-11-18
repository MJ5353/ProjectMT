using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==================================================

// 몬스터 자동 배정. 맵(MapDef) → 버섯(지역 버섯 목록) → 몬스터(MonsterDef)를 이어서 채운다.
//  - 밸런스: MonsterBalanceTable 공식으로 지역 순서·등급·번호에 따라 기입
//  - 드랍: 몬스터가 사는 지역의 버섯 목록에서 추천(목록 앞쪽 = 흔한 버섯). 잡몹은 앞쪽, 엘리트는 뒤쪽, 보스는 전부
//  - 소켓: 드랍 버섯을 무작위 소켓에 꽂는다. 누를 때마다 위치·크기·기울기가 바뀐다
// 지역은 MapDef 스폰 목록에서 찾고, 없으면 번호로 정한다(001~005 첫 지역). 등급은 이름(Boss_)과 번호 순서(잡몹 2 → 엘리트 2 → 보스)
// !!) 공격 판정 프레임·거리·히트스톱 같은 손맛 수치는 건드리지 않는다

public static class MonsterAutoFill
{
    [Flags]
    public enum EPart
    {
        None = 0,
        Balance = 1 << 0,
        Drop = 1 << 1,
        Socket = 1 << 2,
        All = Balance | Drop | Socket,
    }

    public readonly struct DropPick
    {
        public readonly MushroomDef Mushroom;
        public readonly int Min;
        public readonly int Max;
        public readonly float Chance;

        public DropPick(MushroomDef mushroom, int min, int max, float chance)
        {
            Mushroom = mushroom;
            Min = min;
            Max = max;
            Chance = chance;
        }
    }

    public readonly struct SocketPick
    {
        public readonly MushroomDef Mushroom;   // null이면 비운다
        public readonly Renderer Model;
        public readonly float Scale;
        public readonly Vector3 Euler;

        public SocketPick(MushroomDef mushroom, Renderer model, float scale, Vector3 euler)
        {
            Mushroom = mushroom;
            Model = model;
            Scale = scale;
            Euler = euler;
        }
    }

    // 몬스터 한 마리에 넣을 값
    public class Plan
    {
        public MonsterDef Def { get; }
        public MapDef Map { get; set; }
        public EMonsterTier Tier { get; set; }
        public int MapIndex { get; set; }
        public int OrderInTier { get; set; }

        public int Hp { get; set; }
        public int Damage { get; set; }
        public float MoveSpeed { get; set; }
        public float WanderSpeed { get; set; }
        public float SightRange { get; set; }
        public float LoseRange { get; set; }
        public float HitStunTime { get; set; }
        public float WindupTime { get; set; }
        public float AttackCooldown { get; set; }

        public List<DropPick> ListDrop { get; } = new();
        public List<SocketPick> ListSocket { get; } = new();
        public List<string> ListWarning { get; } = new();

        public Plan(MonsterDef def)
        {
            Def = def;
        }
    }

    public const string BalancePath = "Assets/Resources/Data/Balance/MonsterBalance.asset";
    private const string DefPrefix = "Monster_";
    private const string BossMark = "_Boss_";

    // ------------------------------------------------

    // 계획을 보여 주고 확인을 받으면 적용한다. 적용했으면 true
    public static bool Run(IReadOnlyList<MonsterDef> listDef, EPart parts)
    {
        if (listDef == null || listDef.Count == 0 || parts == EPart.None)
            return false;

        MonsterBalanceTable table = LoadTable();
        List<Plan> listPlan = MakePlans(listDef, parts, table, new System.Random());
        string report = Describe(listPlan, parts);
        CLogger.Log("MonsterAutoFill", report);

        string message = Shorten(report, 28) + "\n\n공식은 MonsterBalance 에셋에서 고칩니다. Ctrl+Z로 되돌릴 수 있습니다.";
        if (EditorUtility.DisplayDialog($"몬스터 자동 배정 ({GetPartLabel(parts)})", message, "적용", "취소") == false)
            return false;

        Apply(listPlan, parts);
        return true;
    }

    public static MonsterBalanceTable LoadTable()
    {
        MonsterBalanceTable table = AssetDatabase.LoadAssetAtPath<MonsterBalanceTable>(BalancePath);
        if (table != null)
            return table;

        Directory.CreateDirectory(Path.GetDirectoryName(BalancePath)!);
        table = ScriptableObject.CreateInstance<MonsterBalanceTable>();
        AssetDatabase.CreateAsset(table, BalancePath);
        AssetDatabase.SaveAssets();
        CLogger.Log("MonsterAutoFill", $"밸런스 공식 에셋을 기본값으로 만들었습니다: {BalancePath}", table);
        return table;
    }

    public static string GetPartLabel(EPart parts)
    {
        if (parts == EPart.All)
            return "전부";

        List<string> listLabel = new();
        if ((parts & EPart.Balance) != 0)
            listLabel.Add("밸런스");
        if ((parts & EPart.Drop) != 0)
            listLabel.Add("드랍");
        if ((parts & EPart.Socket) != 0)
            listLabel.Add("버섯 꽂기");

        return string.Join("·", listLabel);
    }

    // ==================================================

    // 계획 ---

    public static List<Plan> MakePlans(IReadOnlyList<MonsterDef> listDef, EPart parts, MonsterBalanceTable table, System.Random random)
    {
        List<MapDef> listMap = DataTools.CollectDefs().OfType<MapDef>().OrderBy(mapDef => mapDef.Order).ToList();
        List<Plan> listPlan = new();

        foreach (MonsterDef def in listDef.OrderBy(def => def.name, StringComparer.Ordinal))
        {
            Plan plan = new Plan(def);
            FillPosition(plan, listMap);
            FillBalance(plan, table);

            // 소켓 버섯도 드랍 추천에서 고르므로 소켓만 할 때도 드랍을 계산한다(적용은 드랍을 고른 경우만)
            List<MushroomDef> listPool = MakeMushroomPool(plan);
            if ((parts & (EPart.Drop | EPart.Socket)) != 0)
                FillDrops(plan, table, listPool);
            if ((parts & EPart.Socket) != 0)
                FillSockets(plan, table, random);

            listPlan.Add(plan);
        }

        return listPlan;
    }

    // 지역·등급·순번
    private static void FillPosition(Plan plan, List<MapDef> listMap)
    {
        MonsterDef def = plan.Def;
        bool hasNo = TryParseNo(def.name, out int no);
        if (hasNo == false)
            plan.ListWarning.Add("이름에 번호(Monster_NNN_Key)가 없어 첫 지역 잡몹으로 계산");

        int slot = hasNo ? (no - 1) % MonsterBalanceTable.MonstersPerMap : 0;
        if (def.name.Contains(BossMark))
        {
            plan.Tier = EMonsterTier.Boss;
            plan.OrderInTier = 0;
        }
        else if (slot >= MonsterBalanceTable.NormalPerMap)
        {
            plan.Tier = EMonsterTier.Elite;
            plan.OrderInTier = Mathf.Min(slot - MonsterBalanceTable.NormalPerMap, 1);
        }
        else
        {
            plan.Tier = EMonsterTier.Normal;
            plan.OrderInTier = slot;
        }

        // 스폰 목록에 있는 지역이 우선, 없으면 번호로 정한 순서의 지역
        int orderByNo = hasNo ? (no - 1) / MonsterBalanceTable.MonstersPerMap + 1 : 1;
        MapDef mapDef = listMap.FirstOrDefault(candidate => candidate.ListSpawn.Any(spawn => spawn != null && spawn.Monster == def));
        if (mapDef == null)
            mapDef = listMap.FirstOrDefault(candidate => candidate.Order == orderByNo);

        plan.Map = mapDef;
        plan.MapIndex = mapDef != null ? Mathf.Max(0, mapDef.Order - 1) : orderByNo - 1;
        if (mapDef == null)
            plan.ListWarning.Add($"지역을 못 찾음(순서 {orderByNo}). 드랍·소켓은 건너뜀");
    }

    private static void FillBalance(Plan plan, MonsterBalanceTable table)
    {
        MonsterBalanceTable.TierStat stat = table.GetTier(plan.Tier);
        plan.Hp = table.CalcHp(plan.Tier, plan.MapIndex, plan.OrderInTier);
        plan.Damage = table.CalcDamage(plan.Tier, plan.MapIndex, plan.OrderInTier);
        plan.MoveSpeed = Round(table.CalcMoveSpeed(plan.Tier, plan.MapIndex));
        plan.WanderSpeed = Round(plan.MoveSpeed * table.WanderSpeedRate);
        plan.SightRange = stat.SightRange;
        plan.LoseRange = stat.SightRange + table.LoseRangeAdd;
        plan.HitStunTime = stat.HitStunTime;
        plan.WindupTime = stat.WindupTime;
        plan.AttackCooldown = stat.AttackCooldown;
    }

    // 지역 버섯 목록에서 이 몬스터가 쓸 버섯. 앞이 대표 버섯
    private static List<MushroomDef> MakeMushroomPool(Plan plan)
    {
        List<MushroomDef> listPool = new();
        if (plan.Map == null)
            return listPool;

        List<MushroomDef> listMapMushroom = plan.Map.ListMushroom.Where(mushroom => mushroom != null).Distinct().ToList();
        int count = listMapMushroom.Count;
        if (count == 0)
        {
            plan.ListWarning.Add($"{plan.Map.name}에 지역 버섯이 비어 있음. 지역 Def의 버섯 목록을 먼저 채울 것");
            return listPool;
        }

        int k = plan.OrderInTier;
        switch (plan.Tier)
        {
            case EMonsterTier.Normal:
                listPool.Add(listMapMushroom[k % count]);
                listPool.Add(listMapMushroom[(k + 1) % count]);
                break;
            case EMonsterTier.Elite:
                listPool.Add(listMapMushroom[(count - 1 - k % count + count) % count]);
                listPool.Add(listMapMushroom[k % count]);
                break;
            default:
                listPool.AddRange(listMapMushroom);
                break;
        }

        return listPool.Distinct().ToList();
    }

    private static void FillDrops(Plan plan, MonsterBalanceTable table, List<MushroomDef> listPool)
    {
        if (listPool.Count == 0)
            return;

        MonsterBalanceTable.TierStat stat = table.GetTier(plan.Tier);
        AddDrop(plan, listPool[0], stat.DropMin, stat.DropMax, 1f);
        if (stat.ExtraDropChance <= 0f)
            return;

        // 잡몹·엘리트는 두 번째 버섯 하나, 보스는 나머지 전부
        int extraEnd = plan.Tier == EMonsterTier.Boss ? listPool.Count : Mathf.Min(2, listPool.Count);
        int extraMin = Mathf.Max(1, stat.DropMin / 2);
        int extraMax = Mathf.Max(1, stat.DropMax / 2);
        for (int i = 1; i < extraEnd; ++i)
        {
            AddDrop(plan, listPool[i], extraMin, extraMax, stat.ExtraDropChance);
        }
    }

    private static void AddDrop(Plan plan, MushroomDef mushroom, int min, int max, float chance)
    {
        if (mushroom.Pickup == null)
        {
            plan.ListWarning.Add($"{mushroom.name} 픽업 없음. 버섯 탭에서 픽업 빌드 먼저");
            return;
        }

        plan.ListDrop.Add(new DropPick(mushroom, min, Mathf.Max(min, max), chance));
    }

    // 드랍 버섯을 무작위 소켓에 꽂는다. 고르지 않은 소켓은 비운다(소켓 이름은 그대로 둔다)
    private static void FillSockets(Plan plan, MonsterBalanceTable table, System.Random random)
    {
        int socketCount = plan.Def.ListMushroom.Count;
        if (socketCount == 0)
        {
            plan.ListWarning.Add("버섯 소켓이 없음(모델의 Socket_* 뼈 확인)");
            return;
        }

        List<MushroomDef> listMushroom = plan.ListDrop.Select(drop => drop.Mushroom).Distinct().ToList();
        if (listMushroom.Count == 0)
            return;

        MonsterBalanceTable.TierStat stat = table.GetTier(plan.Tier);
        int min = Mathf.Clamp(stat.SocketMin, 1, socketCount);
        int max = Mathf.Clamp(stat.SocketMax, min, socketCount);
        int pickCount = random.Next(min, max + 1);

        List<int> listIndex = Enumerable.Range(0, socketCount).OrderBy(_ => random.Next()).ToList();
        HashSet<int> setPicked = new(listIndex.Take(pickCount));

        for (int i = 0; i < socketCount; ++i)
        {
            if (setPicked.Contains(i) == false)
            {
                plan.ListSocket.Add(new SocketPick(null, null, 1f, Vector3.zero));
                continue;
            }

            bool isMain = listMushroom.Count == 1 || random.NextDouble() < table.MainMushroomWeight;
            MushroomDef mushroom = isMain ? listMushroom[0] : listMushroom[1 + random.Next(listMushroom.Count - 1)];
            Renderer model = LoadMushroomRenderer(mushroom);
            if (model == null)
            {
                plan.ListWarning.Add($"{mushroom.ModelName}.fbx 없음. 소켓 비움");
                plan.ListSocket.Add(new SocketPick(null, null, 1f, Vector3.zero));
                continue;
            }

            float scale = Round(Mathf.Lerp(stat.MushroomScale.x, stat.MushroomScale.y, (float)random.NextDouble()));
            Vector3 euler = new Vector3(RandomTilt(random, table.MushroomTilt), RandomTilt(random, table.MushroomTilt), RandomTilt(random, table.MushroomTilt));
            plan.ListSocket.Add(new SocketPick(mushroom, model, scale, euler));
        }
    }

    private static Renderer LoadMushroomRenderer(MushroomDef mushroom)
    {
        GameObject model = BattleSetup.LoadModel(mushroom.ModelName);
        return model != null ? model.GetComponentInChildren<Renderer>(true) : null;
    }

    // 적용 ---

    private static void Apply(List<Plan> listPlan, EPart parts)
    {
        Undo.SetCurrentGroupName("몬스터 자동 배정");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (Plan plan in listPlan)
        {
            using SerializedObject so = new SerializedObject(plan.Def);
            if ((parts & EPart.Balance) != 0)
                ApplyBalance(so, plan);
            if ((parts & EPart.Drop) != 0 && plan.ListDrop.Count > 0)
                ApplyDrops(so, plan);
            if ((parts & EPart.Socket) != 0 && plan.ListSocket.Count > 0)
                ApplySockets(so, plan);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(plan.Def);
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyBalance(SerializedObject so, Plan plan)
    {
        Find(so, nameof(UnitDef.MaxHp)).intValue = plan.Hp;
        Find(so, nameof(UnitDef.HitStunTime)).floatValue = plan.HitStunTime;
        Find(Find(so, nameof(UnitDef.Attack)), nameof(AttackSpec.Damage)).intValue = plan.Damage;
        Find(so, nameof(MonsterDef.MoveSpeed)).floatValue = plan.MoveSpeed;
        Find(so, nameof(MonsterDef.WanderSpeed)).floatValue = plan.WanderSpeed;
        Find(so, nameof(MonsterDef.SightRange)).floatValue = plan.SightRange;
        Find(so, nameof(MonsterDef.LoseRange)).floatValue = plan.LoseRange;
        Find(so, nameof(MonsterDef.WindupTime)).floatValue = plan.WindupTime;
        Find(so, nameof(MonsterDef.AttackCooldown)).floatValue = plan.AttackCooldown;
    }

    private static void ApplyDrops(SerializedObject so, Plan plan)
    {
        SerializedProperty listDrop = so.FindProperty("m_ListDrop");
        listDrop.arraySize = plan.ListDrop.Count;
        for (int i = 0; i < plan.ListDrop.Count; ++i)
        {
            DropPick pick = plan.ListDrop[i];
            SerializedProperty element = listDrop.GetArrayElementAtIndex(i);
            Find(element, nameof(MonsterDef.DropEntry.Pickup)).objectReferenceValue = pick.Mushroom.Pickup;
            Find(element, nameof(MonsterDef.DropEntry.Min)).intValue = pick.Min;
            Find(element, nameof(MonsterDef.DropEntry.Max)).intValue = pick.Max;
            Find(element, nameof(MonsterDef.DropEntry.Chance)).floatValue = pick.Chance;
        }
    }

    private static void ApplySockets(SerializedObject so, Plan plan)
    {
        SerializedProperty listSocket = so.FindProperty("m_ListMushroom");
        int count = Mathf.Min(listSocket.arraySize, plan.ListSocket.Count);
        for (int i = 0; i < count; ++i)
        {
            SocketPick pick = plan.ListSocket[i];
            SerializedProperty element = listSocket.GetArrayElementAtIndex(i);
            Find(element, nameof(MonsterDef.MushroomSocket.Model)).objectReferenceValue = pick.Model;
            Find(element, nameof(MonsterDef.MushroomSocket.Scale)).floatValue = pick.Scale;
            Find(element, nameof(MonsterDef.MushroomSocket.Euler)).vector3Value = pick.Euler;
        }
    }

    // 보고 ---

    public static string Describe(List<Plan> listPlan, EPart parts)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"몬스터 {listPlan.Count}마리 · {GetPartLabel(parts)}");

        foreach (Plan plan in listPlan)
        {
            string mapName = plan.Map != null ? plan.Map.DisplayName : "지역 없음";
            builder.Append($"- {plan.Def.DisplayName}({GetShortName(plan.Def)}) [{mapName}·{GetTierLabel(plan.Tier)}]");
            if ((parts & EPart.Balance) != 0)
                builder.Append($" 체력 {plan.Hp} 공격 {plan.Damage} 속도 {plan.MoveSpeed:0.##}");

            if ((parts & EPart.Drop) != 0 && plan.ListDrop.Count > 0)
            {
                IEnumerable<string> drops = plan.ListDrop.Select(DescribeDrop);
                builder.Append($" | 드랍 {string.Join(", ", drops)}");
            }

            if ((parts & EPart.Socket) != 0 && plan.ListSocket.Count > 0)
            {
                int used = plan.ListSocket.Count(socket => socket.Mushroom != null);
                builder.Append($" | 소켓 {used}/{plan.ListSocket.Count}");
            }

            builder.AppendLine();
            foreach (string warning in plan.ListWarning.Distinct())
            {
                builder.AppendLine($"    !! {warning}");
            }
        }

        return builder.ToString();
    }

    // ==================================================

    public static bool TryParseNo(string defName, out int no)
    {
        no = 0;
        if (defName.StartsWith(DefPrefix, StringComparison.Ordinal) == false)
            return false;

        string[] parts = defName.Split('_');
        return parts.Length >= 3 && int.TryParse(parts[1], out no) && no > 0;
    }

    private static string GetShortName(MonsterDef def)
    {
        return def.name.StartsWith(DefPrefix, StringComparison.Ordinal) ? def.name.Substring(DefPrefix.Length) : def.name;
    }

    private static string GetTierLabel(EMonsterTier tier)
    {
        switch (tier)
        {
            case EMonsterTier.Elite:
                return "엘리트";
            case EMonsterTier.Boss:
                return "보스";
            default:
                return "잡몹";
        }
    }

    private static string DescribeDrop(DropPick drop)
    {
        string chance = drop.Chance < 1f ? $"({Mathf.RoundToInt(drop.Chance * 100f)}%)" : string.Empty;
        return $"{drop.Mushroom.DisplayName} {drop.Min}~{drop.Max}{chance}";
    }

    private static string Shorten(string text, int maxLine)
    {
        string[] lines = text.Split('\n');
        if (lines.Length <= maxLine)
            return text;

        return string.Join("\n", lines.Take(maxLine)) + $"\n… 외 {lines.Length - maxLine}줄은 Console에서 확인";
    }

    private static float Round(float value) => Mathf.Round(value * 100f) / 100f;

    private static float RandomTilt(System.Random random, float range) => Round(((float)random.NextDouble() * 2f - 1f) * range);

    private static SerializedProperty Find(SerializedObject so, string name)
    {
        return so.FindProperty(name) ?? so.FindProperty($"<{name}>k__BackingField");
    }

    private static SerializedProperty Find(SerializedProperty parent, string name)
    {
        return parent.FindPropertyRelative(name) ?? parent.FindPropertyRelative($"<{name}>k__BackingField");
    }
}
