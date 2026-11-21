using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==================================================

// 몬스터 모델 클립으로 SkillDef를 만든다. 기본 공격(Attack_NN 클립)은 Kind Attack, 나머지 공격 클립은 Kind Skill.
// 수치는 기본 공격 값을 복사하고 스킬은 임시 쿨을 넣는다(손맛 수치는 대장이 정한다). 이미 같은 클립의 SkillDef가 있으면 건너뛴다
// !!) ListSkill에 연결한다
// 새 클립은 원래 이름 그대로 Clip에 넣는다. 애니 이름 일괄 정리를 다시 돌리면 표준 이름으로 바뀐다
public static class SkillDefAutoFill
{
    private const float TempCooldown = 5f;

    // 공격이 아닌 클립(이동·반응·연출). 애매한 동작은 일단 여기 둔다
    private static readonly HashSet<string> s_SetNotAttack = new() { "Idle", "Walk", "Run", "Hit", "Death", "Roar", "Down", "PartBreak", "Burrow", "Emerge", "Underground", "Call" };

    // 보류할 몬스터. 북극곰·사우루스 보스는 26.11.18 손으로 SkillDef 연결
    internal static readonly HashSet<string> s_SetHold = new();

    private class Entry
    {
        public MonsterDef Def;
        public string Clip;
        public ESkillKind Kind;
        public string Name;
    }

    // ==================================================

    public static bool Run(IReadOnlyList<MonsterDef> listDef)
    {
        List<SkillDef> listExisting = DataTools.CollectDefs().OfType<SkillDef>().ToList();
        List<Entry> listEntry = new();
        List<string> listWarning = new();
        foreach (MonsterDef def in listDef)
        {
            if (s_SetHold.Contains(def.name))
            {
                listWarning.Add($"{def.name}: 보류");
                continue;
            }

            if (MonsterAutoFill.TryParseNo(def.name, out int no) == false)
            {
                listWarning.Add($"{def.name}: 이름에 번호 없음");
                continue;
            }

            MakeEntries(def, no, listExisting, listEntry, listWarning);
        }

        string report = Describe(listEntry, listWarning);
        CLogger.Log("SkillDefAutoFill", report);
        if (listEntry.Count == 0)
        {
            EditorUtility.DisplayDialog("스킬 Def 만들기", report + "\n만들 것이 없습니다.", "확인");
            return false;
        }

        if (EditorUtility.DisplayDialog("스킬 Def 만들기", report + $"\n스킬 쿨은 임시 {TempCooldown}초. 수치는 기본 공격 복사.", "만들기", "취소") == false)
            return false;

        Apply(listEntry);
        return true;
    }

    private static void MakeEntries(MonsterDef def, int no, List<SkillDef> listExisting, List<Entry> listEntry, List<string> listWarning)
    {
        GameObject model = BattleSetup.LoadModel(MonsterPrefabBuilder.GetModelName(def));
        if (model == null)
        {
            listWarning.Add($"{def.name}: 모델 없음");
            return;
        }

        string prefix = $"Monster_{no:000}_";
        List<SkillDef> listMine = listExisting.Where(s => s.name.StartsWith(prefix)).ToList();
        IEnumerable<string> clips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model))
            .OfType<AnimationClip>()
            .Select(c => c.name)
            .Where(n => n.StartsWith("__preview__") == false && s_SetNotAttack.Contains(n) == false)
            .Distinct()
            .OrderBy(n => n);

        int[] nextNo = { NextNo(listMine, prefix + "Attack_"), NextNo(listMine, prefix + "Skill_") };
        foreach (string clip in clips)
        {
            if (listMine.Any(s => s.Attack.Clip == clip))
                continue;

            ESkillKind kind = clip.StartsWith("Attack_") ? ESkillKind.Attack : ESkillKind.Skill;
            string name = $"{prefix}{kind}_{nextNo[(int)kind]:00}";
            nextNo[(int)kind]++;
            listEntry.Add(new Entry { Def = def, Clip = clip, Kind = kind, Name = name });
        }
    }

    // 이미 있는 번호 다음
    private static int NextNo(List<SkillDef> listMine, string head)
    {
        int max = 0;
        foreach (SkillDef skill in listMine)
        {
            if (skill.name.StartsWith(head) && int.TryParse(skill.name.Substring(head.Length), out int no))
                max = Mathf.Max(max, no);
        }

        return max + 1;
    }

    private static string Describe(List<Entry> listEntry, List<string> listWarning)
    {
        StringBuilder builder = new StringBuilder();
        foreach (Entry entry in listEntry)
        {
            builder.AppendLine($"{entry.Name} ← {entry.Clip}");
        }

        foreach (string warning in listWarning)
        {
            builder.AppendLine($"  !! {warning}");
        }

        return builder.ToString();
    }

    private static void Apply(List<Entry> listEntry)
    {
        Directory.CreateDirectory(EditorPath.SkillDataDir);
        foreach (Entry entry in listEntry)
        {
            AttackSpec source = entry.Def.MainAttack ?? new AttackSpec();
            AttackSpec attack = JsonUtility.FromJson<AttackSpec>(JsonUtility.ToJson(source)); // 값 복사

            SkillDef skill = ScriptableObject.CreateInstance<SkillDef>();
            skill.EditorSetup(entry.Kind, attack);
            skill.EditorSetIdentity(entry.Name, entry.Clip);
            AssetDatabase.CreateAsset(skill, $"{EditorPath.SkillDataDir}{entry.Name}.asset");

            using (SerializedObject so = new SerializedObject(skill))
            {
                so.FindProperty("<Attack>k__BackingField.<Clip>k__BackingField").stringValue = entry.Clip;
                if (entry.Kind == ESkillKind.Skill)
                    so.FindProperty("<Attack>k__BackingField.<Cooldown>k__BackingField").floatValue = TempCooldown;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            using SerializedObject defSo = new SerializedObject(entry.Def);
            SerializedProperty listSkill = defSo.FindProperty("<ListSkill>k__BackingField");
            listSkill.arraySize++;
            listSkill.GetArrayElementAtIndex(listSkill.arraySize - 1).objectReferenceValue = skill;
            defSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(entry.Def);
        }

        AssetDatabase.SaveAssets();
        DataTools.RefreshDatabase(false);
    }
}
