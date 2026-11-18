using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==================================================

// 애니 이름 일괄 정리. SkillDef 이름(…_Attack_NN·…_Skill_NN)이 곧 표준 클립 이름이다.
//  1. 플레이어 기본 공격 SkillDef가 없으면 ListAttack으로 만든다(Player_Attack_NN)
//  2. 원래 클립 → 표준 이름을 AnimNameTable.txt에 추가한다(지우지 않는다. 가져오기 때마다 이 표로 바꾼다)
//  3. SkillDef.Attack.Clip을 표준 이름으로, 기본 공격 수치는 ListAttack 값으로 맞추고 ListAttack을 비운 뒤 ListSkill에 기본 공격 → 스킬 순으로 둔다
//  4. 모델을 다시 가져오고 애니메이터를 다시 만든다(프리팹은 그대로)
// !!) 보류 몬스터(SkillDefAutoFill.s_SetHold)와 SkillDef가 없는 몬스터는 건너뛴다
public static class AnimNameMigration
{
    private const string PlayerPrefix = "Player_";

    private class Plan
    {
        public UnitDef Unit;
        public string ModelName;
        public string Prefix;
        public readonly List<SkillDef> ListSkill = new();                 // 기본 공격 → 스킬, 이름순
        public readonly Dictionary<string, string> DicRename = new();     // 원래 클립 → 표준 이름(새로 추가할 것만)
        public readonly Dictionary<SkillDef, string> DicClip = new();     // SkillDef → 바꿀 Clip
        public readonly List<AttackSpec> ListNewPlayerAttack = new();     // 새로 만들 플레이어 기본 공격
    }

    // ==================================================

    [MenuItem("MushTaker/애니 이름 일괄 정리", false, 30)]
    public static void Run()
    {
        List<DataDef> listDef = DataTools.CollectDefs();
        List<SkillDef> listSkill = listDef.OfType<SkillDef>().ToList();
        List<Plan> listPlan = new();
        foreach (UnitDef unit in listDef.OfType<UnitDef>())
        {
            Plan plan = MakePlan(unit, listSkill);
            if (plan != null)
                listPlan.Add(plan);
        }

        if (listPlan.Count == 0)
        {
            EditorUtility.DisplayDialog("애니 이름 일괄 정리", "정리할 유닛이 없습니다.", "확인");
            return;
        }

        string report = Describe(listPlan);
        CLogger.Log("AnimNameMigration", report);
        if (EditorUtility.DisplayDialog("애니 이름 일괄 정리", report + "\n모델을 다시 가져오고 애니메이터를 다시 만듭니다. 데이터는 Ctrl+Z로 되돌릴 수 있지만 이름 표와 모델은 되돌아가지 않습니다.", "적용", "취소") == false)
            return;

        Apply(listPlan);
    }

    private static Plan MakePlan(UnitDef unit, List<SkillDef> listSkill)
    {
        if (SkillDefAutoFill.s_SetHold.Contains(unit.name))
            return null;

        Plan plan = new Plan { Unit = unit };
        if (unit is MonsterDef monster)
        {
            if (MonsterAutoFill.TryParseNo(monster.name, out int no) == false)
                return null;

            plan.ModelName = MonsterPrefabBuilder.GetModelName(monster);
            plan.Prefix = $"Monster_{no:000}_";
        }
        else if (unit is PlayerDef)
        {
            plan.ModelName = "Player";
            plan.Prefix = PlayerPrefix;
        }
        else
        {
            return null;
        }

        plan.ListSkill.AddRange(listSkill.Where(s => s.name.StartsWith(plan.Prefix))
            .OrderBy(s => s.Kind)
            .ThenBy(s => s.name));

        // 플레이어는 기본 공격 SkillDef를 여기서 만든다(Player_Attack_01부터 ListAttack 순서)
        if (unit is PlayerDef && plan.ListSkill.Any(s => s.Kind == ESkillKind.Attack) == false)
            plan.ListNewPlayerAttack.AddRange(unit.ListAttack);

        if (plan.ListSkill.Count == 0 && plan.ListNewPlayerAttack.Count == 0)
            return null;

        Dictionary<string, string> dicTable = VoxelImportSettings.LoadAnimNames(plan.ModelName);
        foreach (SkillDef skill in plan.ListSkill)
        {
            string standardName = skill.name.Substring(plan.Prefix.Length);
            string clip = skill.Attack.Clip;
            if (clip == standardName)
                continue;

            // 같은 클립을 둘이 쓰면(플레이어 위로 치기) 먼저 정한 이름을 같이 쓴다
            if (dicTable.TryGetValue(clip, out string tableName) || plan.DicRename.TryGetValue(clip, out tableName))
            {
                plan.DicClip[skill] = tableName;
                continue;
            }

            plan.DicRename[clip] = standardName;
            plan.DicClip[skill] = standardName;
        }

        for (int i = 0; i < plan.ListNewPlayerAttack.Count; i++)
        {
            string clip = plan.ListNewPlayerAttack[i].Clip;
            if (plan.DicRename.ContainsKey(clip) == false && dicTable.ContainsKey(clip) == false)
                plan.DicRename[clip] = $"Attack_{i + 1:00}";
        }

        return plan;
    }

    private static string Describe(List<Plan> listPlan)
    {
        StringBuilder builder = new StringBuilder();
        foreach (Plan plan in listPlan)
        {
            string rename = string.Join(", ", plan.DicRename.Select(p => $"{p.Key}→{p.Value}"));
            string created = plan.ListNewPlayerAttack.Count > 0 ? $" / 새 기본 공격 {plan.ListNewPlayerAttack.Count}개" : "";
            builder.AppendLine($"{plan.Unit.name}: {(rename.Length > 0 ? rename : "이름 그대로")}{created} / ListAttack {plan.Unit.ListAttack.Count}칸 비움");
        }

        return builder.ToString();
    }

    private static void Apply(List<Plan> listPlan)
    {
        Undo.SetCurrentGroupName("애니 이름 일괄 정리");
        int undoGroup = Undo.GetCurrentGroup();
        List<string> listTableLine = new();

        foreach (Plan plan in listPlan)
        {
            CreatePlayerAttacks(plan);

            foreach (var pair in plan.DicRename)
            {
                listTableLine.Add($"{plan.ModelName}|{pair.Key}|{pair.Value}");
            }

            foreach (SkillDef skill in plan.ListSkill)
            {
                ApplySkill(plan, skill);
            }

            using SerializedObject so = new SerializedObject(plan.Unit);
            SerializedProperty listAttack = so.FindProperty("<ListAttack>k__BackingField");
            listAttack.arraySize = 0;
            SerializedProperty listSkill = so.FindProperty("<ListSkill>k__BackingField");
            listSkill.arraySize = plan.ListSkill.Count;
            for (int i = 0; i < plan.ListSkill.Count; i++)
            {
                listSkill.GetArrayElementAtIndex(i).objectReferenceValue = plan.ListSkill[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(plan.Unit);
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();

        if (listTableLine.Count > 0)
            File.AppendAllLines(VoxelImportSettings.AnimNameTablePath, listTableLine);

        foreach (Plan plan in listPlan)
        {
            GameObject model = BattleSetup.LoadModel(plan.ModelName);
            if (model == null)
            {
                CLogger.Warning("AnimNameMigration", $"모델이 없어 다시 가져오기 건너뜀: {plan.ModelName}");
                continue;
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(model), ImportAssetOptions.ForceUpdate);
            BattleSetup.BuildController(BattleSetup.LoadModel(plan.ModelName));
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);
        CLogger.Log("AnimNameMigration", $"애니 이름 정리 완료: 유닛 {listPlan.Count}개, 표 {listTableLine.Count}줄 추가. 데이터 검사로 확인하세요.");
    }

    // 플레이어 ListAttack 칸마다 Player_Attack_NN. 같은 클립을 쓰는 칸은 첫 칸의 표준 이름을 쓴다
    private static void CreatePlayerAttacks(Plan plan)
    {
        if (plan.ListNewPlayerAttack.Count == 0)
            return;

        Directory.CreateDirectory(SkillDefAutoFill.SkillDataDir);
        for (int i = 0; i < plan.ListNewPlayerAttack.Count; i++)
        {
            AttackSpec attack = JsonUtility.FromJson<AttackSpec>(JsonUtility.ToJson(plan.ListNewPlayerAttack[i]));
            string name = $"{PlayerPrefix}Attack_{i + 1:00}";
            SkillDef skill = ScriptableObject.CreateInstance<SkillDef>();
            skill.EditorSetup(ESkillKind.Attack, attack);
            skill.EditorSetIdentity(name, name);
            AssetDatabase.CreateAsset(skill, $"{SkillDefAutoFill.SkillDataDir}{name}.asset");

            plan.DicClip[skill] = plan.DicRename.TryGetValue(attack.Clip, out string standardName) ? standardName : attack.Clip;
            plan.ListSkill.Add(skill);
        }

        plan.ListSkill.Sort((a, b) => a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : string.CompareOrdinal(a.name, b.name));
    }

    // 기본 공격은 ListAttack의 같은 클립 값으로 다시 맞추고(자동 생성 뒤 바뀐 값), Clip을 표준 이름으로
    private static void ApplySkill(Plan plan, SkillDef skill)
    {
        // 방금 만든 플레이어 공격은 이미 칸별 값이라 건너뛴다(같은 클립 칸이 여럿)
        bool isCopied = plan.ListNewPlayerAttack.Count > 0;
        AttackSpec source = skill.Kind == ESkillKind.Attack && isCopied == false ? plan.Unit.ListAttack.FirstOrDefault(a => a.Clip == skill.Attack.Clip) : null;
        if (source != null)
        {
            Undo.RecordObject(skill, "애니 이름 일괄 정리");
            skill.EditorSetup(skill.Kind, JsonUtility.FromJson<AttackSpec>(JsonUtility.ToJson(source)));
        }

        if (plan.DicClip.TryGetValue(skill, out string clip))
        {
            using SerializedObject so = new SerializedObject(skill);
            so.FindProperty("<Attack>k__BackingField.<Clip>k__BackingField").stringValue = clip;
            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(skill);
    }
}
