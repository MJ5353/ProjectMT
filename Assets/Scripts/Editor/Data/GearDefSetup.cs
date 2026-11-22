using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// 정비 강화 노드(GearNode_*)·엠블렘(Emblem_*) 에셋을 아래 표대로 만든다. 이미 있는 에셋은 건드리지 않고 없는 것만 만든다(값은 에셋에서 고친다).
// 트리: 5줄(공격력·공격 속도·회피·방어·최대 체력) × 5노드, 이웃 줄 사이 연계 노드 4, 줄 끝 극의 노드 5 = 34개.
// 열(Slot.x)은 줄이 0·2·4·6·8, 연계가 그 사이 홀수. 행(Slot.y)은 0이 아래, 연계는 조건 노드 행 + 0.5, 극의 노드는 5

public static class GearDefSetup
{
    private const string NodePrefix = "GearNode_";
    private const string EmblemPrefix = "Emblem_";
    private const string PassivePrefix = "Passive_";
    private const int LineLength = 5;

    private static readonly int[] s_LineCost = { 50, 100, 200, 400, 800 }; // [mj] todo) 가안. 줄 노드 1~5번째 골드
    private static readonly string[] s_Roman = { "I", "II", "III", "IV", "V" };

    // 줄: 열, 능력치, 키, 이름, 노드 하나 값
    private static readonly (int Column, EStatType Stat, string Key, string Name, float Value)[] s_Lines =
    {
        (0, EStatType.Attack, "Attack", "공격력", 0.10f),
        (2, EStatType.AttackSpeed, "AttackSpeed", "공격 속도", 0.06f),
        (4, EStatType.Dodge, "Dodge", "회피", 0.12f),
        (6, EStatType.Defense, "Defense", "방어", 0.06f),
        (8, EStatType.MaxHp, "MaxHp", "최대 체력", 0.20f),
    };

    // 스페셜 노드
    private class SpecialRow
    {
        public string Key;
        public string Name;
        public string Desc;
        public Vector2 Slot;
        public int Cost;
        public StatModifier Modifier;
        public Func<PassiveDef> Passive;    // 새 패시브 에셋을 만든다(값까지). 이미 있으면 부르지 않는다
        public (string LineKey, int Level)[] Requires;
    }

    private static readonly SpecialRow[] s_Specials =
    {
        // 연계 노드(이웃한 두 줄)
        new() { Key = "PartHunter", Name = "버섯 사냥꾼", Desc = "버섯 부위를 더 잘 깬다.", Slot = new Vector2(1, 1.5f), Cost = 300,
            Modifier = Rate(EStatType.PartDamage, 0.25f), Requires = new[] { ("Attack", 2), ("AttackSpeed", 2) } },
        new() { Key = "RunSlash", Name = "질주 베기", Desc = "달리다 베기 피해 x1.5", Slot = new Vector2(3, 2.5f), Cost = 600,
            Passive = () => SkillBoost("Player_Attack_06", 1.5f, 1f), Requires = new[] { ("AttackSpeed", 3), ("Dodge", 3) } },
        new() { Key = "Insurance", Name = "상해보험", Desc = "맞으면 골드 동전 5~10개를 떨군다.", Slot = new Vector2(5, 0.5f), Cost = 300,
            Passive = () => Create<PassiveGoldOnHitDef>(passive => passive.EditorSetup(new Vector2Int(5, 10))), Requires = new[] { ("Dodge", 1), ("Defense", 1) } },
        new() { Key = "LastStand", Name = "든든한 뿌리", Desc = "체력 30% 이하일 때 받는 피해 -30%", Slot = new Vector2(7, 2.5f), Cost = 600,
            Passive = () => Create<PassiveLowHpGuardDef>(passive => passive.EditorSetup(0.3f, 0.3f)), Requires = new[] { ("Defense", 3), ("MaxHp", 3) } },

        // 극의 노드(줄 끝)
        new() { Key = "Barbarian", Name = "바바리안", Desc = "막타 내려찍기 피해 x1.5, 범위 x1.3", Slot = new Vector2(0, 5), Cost = 1500,
            Passive = () => SkillBoost("Player_Attack_05", 1.5f, 1.3f), Requires = new[] { ("Attack", 5) } },
        new() { Key = "SwiftFeet", Name = "탁월한 속도", Desc = "이동 속도가 한 단계 빨라진다.", Slot = new Vector2(2, 5), Cost = 1500,
            Modifier = Rate(EStatType.MoveSpeed, 0.15f), Requires = new[] { ("AttackSpeed", 5) } },
        new() { Key = "Gale", Name = "질풍", Desc = "대시로 더 멀리 간다.", Slot = new Vector2(4, 5), Cost = 1500,
            Modifier = Rate(EStatType.DashDistance, 0.3f), Requires = new[] { ("Dodge", 5) } },
        new() { Key = "IronCap", Name = "강철 갓", Desc = "약한 공격에는 움찔하지 않는다.", Slot = new Vector2(6, 5), Cost = 1500,
            Modifier = new StatModifier(EStatType.BreakResist, EStatModType.Add, 1f), Requires = new[] { ("Defense", 5) } },
        new() { Key = "SporeRegen", Name = "포자 재생", Desc = "버섯을 주울 때 체력 3 회복", Slot = new Vector2(8, 5), Cost = 1500,
            Passive = () => Create<PassiveHealOnPickupDef>(passive => passive.EditorSetup(3)), Requires = new[] { ("MaxHp", 5) } },
    };

    // 엠블렘: 필요 노드 수, 이름, 보너스(누적). 그림은 리소스 작업 후 Icon에 직접 넣는다
    private static readonly (int Need, string Name, StatModifier[] Bonus)[] s_Emblems =
    {
        (0, "새싹 채집꾼", new StatModifier[0]),
        (8, "숲의 채집꾼", new[] { Rate(EStatType.MaxHp, 0.05f) }),
        (16, "밤의 사냥꾼", new[] { Rate(EStatType.Attack, 0.05f) }),
        (24, "포자 수호자", new[] { Rate(EStatType.Defense, 0.05f) }),
        (34, "버섯왕", new[]
        {
            Rate(EStatType.Attack, 0.05f), Rate(EStatType.MaxHp, 0.05f), Rate(EStatType.Dodge, 0.05f), Rate(EStatType.Defense, 0.05f), Rate(EStatType.AttackSpeed, 0.05f),
        }),
    };

    // ------------------------------------------------

    [MenuItem("MushTaker/초기 설정/정비 노드·엠블렘 만들기", false, 30)]
    public static void CreateAll()
    {
        Directory.CreateDirectory(EditorPath.GearDataDir);
        Directory.CreateDirectory(EditorPath.PassiveDataDir);

        int created = 0;
        Dictionary<string, GearNodeDef> dicNode = new();
        Dictionary<GearNodeDef, List<GearNodeDef>> dicRequire = new();

        // 줄 노드: 001부터 줄 순서대로
        int number = 1;
        foreach (var line in s_Lines)
        {
            for (int level = 1; level <= LineLength; level++)
            {
                string id = $"{NodePrefix}{number:000}_{line.Key}{level}";
                GearNodeDef node = LoadOrCreate<GearNodeDef>(id, ref created, out bool isNew);
                if (isNew)
                {
                    node.EditorSetIdentity(id, $"{line.Name} {s_Roman[level - 1]}");
                    node.EditorSetup(new List<StatModifier> { Rate(line.Stat, line.Value) }, null, s_LineCost[level - 1], new Vector2(line.Column, level - 1));
                    dicRequire[node] = new List<GearNodeDef>();
                    if (level > 1)
                        dicRequire[node].Add(dicNode[LineKey(line.Key, level - 1)]);
                }

                dicNode[LineKey(line.Key, level)] = node;
                number++;
            }
        }

        // 스페셜 노드. 패시브가 있으면 Passive_NNN_Key 에셋을 먼저 만든다(번호는 패시브 있는 노드 순)
        int passiveNumber = 1;
        foreach (SpecialRow row in s_Specials)
        {
            string id = $"{NodePrefix}{number:000}_{row.Key}";
            number++;

            PassiveDef passive = null;
            if (row.Passive != null)
            {
                passive = LoadOrCreatePassive($"{PassivePrefix}{passiveNumber:000}_{row.Key}", row.Name, row.Desc, row.Passive, ref created);
                passiveNumber++;
            }

            GearNodeDef node = LoadOrCreate<GearNodeDef>(id, ref created, out bool isNew);
            if (isNew == false)
                continue;

            node.EditorSetIdentity(id, row.Name, row.Desc);
            node.EditorSetup(row.Modifier != null ? new List<StatModifier> { row.Modifier } : new List<StatModifier>(), passive, row.Cost, row.Slot);
            dicRequire[node] = row.Requires.Select(require => dicNode[LineKey(require.LineKey, require.Level)]).ToList();
        }

        // 조건은 노드를 다 만든 뒤에 잇는다(새로 만든 것만)
        foreach (var pair in dicRequire)
        {
            pair.Key.EditorSetRequire(pair.Value);
            EditorUtility.SetDirty(pair.Key);
        }

        // 엠블렘
        for (int i = 0; i < s_Emblems.Length; i++)
        {
            var row = s_Emblems[i];
            string id = $"{EmblemPrefix}{i + 1:000}";
            EmblemDef emblem = LoadOrCreate<EmblemDef>(id, ref created, out bool isNew);
            if (isNew == false)
                continue;

            emblem.EditorSetIdentity(id, row.Name);
            emblem.EditorSetup(row.Need, row.Bonus.ToList());
            EditorUtility.SetDirty(emblem);
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(true);
        CLogger.Log("GearDefSetup", $"정비 데이터 새로 만듦: {created}개 ({EditorPath.GearDataDir})");
    }

    // ==================================================

    private static string LineKey(string key, int level) => $"{key}{level}";

    private static T LoadOrCreate<T>(string id, ref int created, out bool isNew) where T : DataDef
    {
        string path = $"{EditorPath.GearDataDir}{id}.asset";
        T def = AssetDatabase.LoadAssetAtPath<T>(path);
        isNew = def == null;
        if (isNew == false)
            return def;

        def = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(def, path);
        created++;
        return def;
    }

    private static StatModifier Rate(EStatType stat, float value) => new(stat, EStatModType.Rate, value);

    // 패시브 ---

    private static T Create<T>(Action<T> setup) where T : PassiveDef
    {
        T passive = ScriptableObject.CreateInstance<T>();
        setup(passive);
        return passive;
    }

    private static PassiveDef SkillBoost(string skillID, float damageRate, float radiusRate)
    {
        SkillDef skill = AssetDatabase.LoadAssetAtPath<SkillDef>($"{EditorPath.SkillDataDir}{skillID}.asset");
        if (skill == null)
            CLogger.Warning("GearDefSetup", $"공격 {skillID}이 없습니다. 패시브 에셋에서 Skill을 채우세요.");

        return Create<PassiveSkillBoostDef>(passive => passive.EditorSetup(skill, damageRate, radiusRate));
    }

    // 있으면 그대로 쓰고, 없으면 create로 만들어 저장한다
    private static PassiveDef LoadOrCreatePassive(string id, string name, string desc, Func<PassiveDef> create, ref int created)
    {
        string path = $"{EditorPath.PassiveDataDir}{id}.asset";
        PassiveDef passive = AssetDatabase.LoadAssetAtPath<PassiveDef>(path);
        if (passive != null)
            return passive;

        passive = create();
        passive.EditorSetIdentity(id, name, desc);
        AssetDatabase.CreateAsset(passive, path);
        created++;

        return passive;
    }
}
