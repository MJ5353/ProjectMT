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
    private const int LineLength = 5;

    private static readonly int[] s_LineCost = { 50, 100, 200, 400, 800 }; // [mj] todo) 가안. 줄 노드 1~5번째 골드
    private static readonly string[] s_Roman = { "I", "II", "III", "IV", "V" };

    // 줄: 열, 능력치, 키, 이름, 노드 하나 값
    private static readonly (int Column, EGearStat Stat, string Key, string Name, float Value)[] s_Lines =
    {
        (0, EGearStat.Attack, "Attack", "공격력", 0.10f),
        (2, EGearStat.AttackSpeed, "AttackSpeed", "공격 속도", 0.06f),
        (4, EGearStat.Dodge, "Dodge", "회피", 0.12f),
        (6, EGearStat.Defense, "Defense", "방어", 0.06f),
        (8, EGearStat.MaxHp, "MaxHp", "최대 체력", 0.20f),
    };

    // 스페셜 노드
    private class SpecialRow
    {
        public string Key;
        public string Name;
        public string Desc;
        public Vector2 Slot;
        public int Cost;
        public EGearStat Stat;
        public float StatValue;
        public EGearSpecial Special;
        public float Value;
        public float Value2;
        public Vector2Int Range;
        public string SkillID;
        public (string LineKey, int Level)[] Requires;
    }

    private static readonly SpecialRow[] s_Specials =
    {
        // 연계 노드(이웃한 두 줄)
        new() { Key = "PartHunter", Name = "버섯 사냥꾼", Desc = "버섯 부위를 더 잘 깬다.", Slot = new Vector2(1, 1.5f), Cost = 300,
            Stat = EGearStat.PartDamage, StatValue = 0.25f, Requires = new[] { ("Attack", 2), ("AttackSpeed", 2) } },
        new() { Key = "RunSlash", Name = "질주 베기", Desc = "달리다 베기 피해 x1.5", Slot = new Vector2(3, 2.5f), Cost = 600,
            Special = EGearSpecial.SkillBoost, Value = 1.5f, Value2 = 1f, SkillID = "Player_Attack_06", Requires = new[] { ("AttackSpeed", 3), ("Dodge", 3) } },
        new() { Key = "Insurance", Name = "상해보험", Desc = "맞으면 골드 5~10을 받는다.", Slot = new Vector2(5, 0.5f), Cost = 300,
            Special = EGearSpecial.Insurance, Range = new Vector2Int(5, 10), Requires = new[] { ("Dodge", 1), ("Defense", 1) } },
        new() { Key = "LastStand", Name = "든든한 뿌리", Desc = "체력 30% 이하일 때 받는 피해 -30%", Slot = new Vector2(7, 2.5f), Cost = 600,
            Special = EGearSpecial.LastStand, Value = 0.3f, Value2 = 0.3f, Requires = new[] { ("Defense", 3), ("MaxHp", 3) } },

        // 극의 노드(줄 끝)
        new() { Key = "Barbarian", Name = "바바리안", Desc = "막타 내려찍기 피해 x1.5, 범위 x1.3", Slot = new Vector2(0, 5), Cost = 1500,
            Special = EGearSpecial.SkillBoost, Value = 1.5f, Value2 = 1.3f, SkillID = "Player_Attack_05", Requires = new[] { ("Attack", 5) } },
        new() { Key = "SwiftFeet", Name = "탁월한 속도", Desc = "이동 속도가 한 단계 빨라진다.", Slot = new Vector2(2, 5), Cost = 1500,
            Stat = EGearStat.MoveSpeed, StatValue = 0.15f, Requires = new[] { ("AttackSpeed", 5) } },
        new() { Key = "Gale", Name = "질풍", Desc = "대시로 더 멀리 간다.", Slot = new Vector2(4, 5), Cost = 1500,
            Stat = EGearStat.DashDistance, StatValue = 0.3f, Requires = new[] { ("Dodge", 5) } },
        new() { Key = "IronCap", Name = "강철 갓", Desc = "약한 공격에는 움찔하지 않는다.", Slot = new Vector2(6, 5), Cost = 1500,
            Stat = EGearStat.BreakResist, StatValue = 1f, Requires = new[] { ("Defense", 5) } },
        new() { Key = "SporeRegen", Name = "포자 재생", Desc = "버섯을 주울 때 체력 3 회복", Slot = new Vector2(8, 5), Cost = 1500,
            Special = EGearSpecial.SporeRegen, Value = 3f, Requires = new[] { ("MaxHp", 5) } },
    };

    // 엠블렘: 필요 노드 수, 이름, 색, 보너스(누적)
    private static readonly (int Need, string Name, Color Tint, (EGearStat Stat, float Value)[] Bonus)[] s_Emblems =
    {
        (0, "새싹 채집꾼", new Color(0.62f, 0.62f, 0.62f), new (EGearStat, float)[0]),
        (8, "숲의 채집꾼", new Color(0.48f, 0.79f, 0.31f), new[] { (EGearStat.MaxHp, 0.05f) }),
        (16, "밤의 사냥꾼", new Color(0.31f, 0.64f, 0.88f), new[] { (EGearStat.Attack, 0.05f) }),
        (24, "포자 수호자", new Color(0.71f, 0.37f, 0.88f), new[] { (EGearStat.Defense, 0.05f) }),
        (34, "버섯왕", new Color(0.95f, 0.76f, 0.31f), new[]
        {
            (EGearStat.Attack, 0.05f), (EGearStat.MaxHp, 0.05f), (EGearStat.Dodge, 0.05f), (EGearStat.Defense, 0.05f), (EGearStat.AttackSpeed, 0.05f),
        }),
    };

    // ------------------------------------------------

    [MenuItem("MushTaker/초기 설정/정비 노드·엠블렘 만들기", false, 30)]
    public static void CreateAll()
    {
        Directory.CreateDirectory(EditorPath.GearDataDir);

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
                    node.EditorSetup(line.Stat, line.Value, EGearSpecial.None, 0f, 0f, Vector2Int.zero, null, s_LineCost[level - 1], new Vector2(line.Column, level - 1));
                    dicRequire[node] = new List<GearNodeDef>();
                    if (level > 1)
                        dicRequire[node].Add(dicNode[LineKey(line.Key, level - 1)]);
                }

                dicNode[LineKey(line.Key, level)] = node;
                number++;
            }
        }

        // 스페셜 노드
        foreach (SpecialRow row in s_Specials)
        {
            string id = $"{NodePrefix}{number:000}_{row.Key}";
            number++;

            GearNodeDef node = LoadOrCreate<GearNodeDef>(id, ref created, out bool isNew);
            if (isNew == false)
                continue;

            SkillDef skill = null;
            if (row.SkillID.E_IsNullOrEmpty() == false)
            {
                skill = AssetDatabase.LoadAssetAtPath<SkillDef>($"{EditorPath.SkillDataDir}{row.SkillID}.asset");
                if (skill == null)
                    CLogger.Warning("GearDefSetup", $"{id}: 공격 {row.SkillID}이 없습니다. 에셋에서 TargetSkill을 채우세요.");
            }

            node.EditorSetIdentity(id, row.Name, row.Desc);
            node.EditorSetup(row.Stat, row.StatValue, row.Special, row.Value, row.Value2, row.Range, skill, row.Cost, row.Slot);
            dicRequire[node] = row.Requires.Select(require => dicNode[LineKey(require.LineKey, require.Level)]).ToList();
        }

        // 조건은 노드를 다 만든 뒤에 잇는다(새로 만든 것만)
        foreach (var pair in dicRequire)
        {
            pair.Key.EditorSetRequire(pair.Value);
            EditorUtility.SetDirty(pair.Key);
        }

        // 엠블렘
        Sprite emblemSprite = UIBuilder.LoadSprite("UI_Slot_Common"); // [mj] todo) 엠블렘 그림 전 임시
        for (int i = 0; i < s_Emblems.Length; i++)
        {
            var row = s_Emblems[i];
            string id = $"{EmblemPrefix}{i + 1:000}";
            EmblemDef emblem = LoadOrCreate<EmblemDef>(id, ref created, out bool isNew);
            if (isNew == false)
                continue;

            emblem.EditorSetIdentity(id, row.Name);
            emblem.EditorSetup(row.Need, row.Tint, row.Bonus.Select(bonus => new EmblemDef.Bonus(bonus.Stat, bonus.Value)).ToList());
            SetIcon(emblem, emblemSprite);
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

    // DataDef.Icon은 private set이라 직렬화로 넣는다
    private static void SetIcon(DataDef def, Sprite sprite)
    {
        SerializedObject so = new SerializedObject(def);
        SerializedProperty prop = so.FindProperty("<Icon>k__BackingField");
        if (prop == null)
            return;

        prop.objectReferenceValue = sprite;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
