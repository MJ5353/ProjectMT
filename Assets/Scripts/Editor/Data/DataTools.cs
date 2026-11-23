using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 데이터베이스 갱신·검사. 플레이 버튼을 누를 때도 자동으로 돌아서 새 SO를 만들기만 하면 게임에 잡힌다
[InitializeOnLoad]
public static class DataTools
{
    static DataTools()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RefreshDatabase(false);
        };
    }

    private static readonly Regex s_SkillNameRule = new(@"^(Monster_\d{3}|Player)_(Attack|Skill)_\d{2}$");

    public static GameDatabase RefreshDatabase(bool isVerbose)
    {
        GameDatabase db = AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPath.GameDatabaseAsset);
        if (db == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EditorPath.GameDatabaseAsset)!);

            db = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(db, EditorPath.GameDatabaseAsset);
        }

        List<DataDef> listDef = CollectDefs();
        db.EditorSetList(listDef);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);

        bool isValid = Validate(listDef, isVerbose);
        if (isVerbose && isValid)
            CLogger.Log("DataTools", $"데이터베이스 갱신 완료: {listDef.Count}개");

        return db;
    }

    public static List<DataDef> CollectDefs()
    {
        return AssetDatabase.FindAssets("t:DataDef", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<DataDef>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(d => d != null)
            .OrderBy(d => d.GetType().Name).ThenBy(d => d.ID)
            .ToList();
    }

    // 데이터 편집기 "데이터 검사" 버튼. 공격 검사는 모델을 읽어서 플레이 때 자동 검사에는 넣지 않는다(경고만)
    public static bool ValidateAll()
    {
        List<DataDef> listDef = CollectDefs();
        CheckAttacks(listDef);
        return Validate(listDef, true);
    }

    // 공격 칸: 애니 상태(= 클립 이름)가 모델에 있는지, 기본 공격에 개별 쿨이 없는지, 스킬 이름 규칙
    private static void CheckAttacks(List<DataDef> listDef)
    {
        foreach (SkillDef skill in listDef.OfType<SkillDef>())
        {
            if (s_SkillNameRule.IsMatch(skill.name) == false)
                CLogger.Warning("DataTools", $"스킬 이름 규칙(Monster_NNN_Skill_NN / _Attack_NN)과 다름: {skill.name}", skill);
        }

        foreach (UnitDef unit in listDef.OfType<UnitDef>())
        {
            string modelName = unit is MonsterDef monster ? MonsterPrefabBuilder.GetModelName(monster) : "Player";
            HashSet<string> setClip = LoadClipNames(modelName);
            if (setClip == null)
            {
                CLogger.Warning("DataTools", $"모델이 없어 애니 검사 건너뜀: {unit.name} → {modelName}", unit);
                continue;
            }

            // 분노 스킬(MonsterDef.ListEnrageSkill)도 애니 상태가 있어야 한다
            IEnumerable<SkillDef> skills = unit is MonsterDef enrageOwner ? unit.ListSkill.Concat(enrageOwner.ListEnrageSkill) : unit.ListSkill;
            foreach (SkillDef skill in skills)
            {
                if (skill == null)
                {
                    CLogger.Warning("DataTools", $"스킬 칸이 비어 있음: {unit.name}", unit);
                    continue;
                }

                CheckClip(unit, skill.Attack, setClip, skill.name);
                if (skill.Kind == ESkillKind.Attack && skill.Attack.Cooldown > 0f)
                    CLogger.Warning("DataTools", $"기본 공격 SkillDef에 개별 쿨({skill.Attack.Cooldown})이 있음: {skill.name}. 쿨이 있으면 Kind를 Skill로", skill);
            }
        }
    }

    private static void CheckClip(UnitDef unit, AttackSpec attack, HashSet<string> setClip, string label)
    {
        if (attack != null && setClip.Contains(attack.Clip) == false)
            CLogger.Warning("DataTools", $"애니 상태 없음: {unit.name} {label} '{attack.Clip}'", unit);
    }

    // 모델 FBX의 클립 이름(애니메이터 상태 이름과 같다). 모델이 없으면 null
    private static HashSet<string> LoadClipNames(string modelName)
    {
        GameObject model = BattleSetup.LoadModel(modelName);
        if (model == null)
            return null;

        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model))
            .OfType<AnimationClip>()
            .Where(c => c.name.StartsWith("__preview__") == false)
            .Select(c => c.name)
            .ToHashSet();
    }

    // AI 프리셋 조립(MonsterDef.SetAI): AI 코어만·중복 없이, 기본·피격 뒤 코어가 목록에 있는지. 서브는 코어가 정한다(AICore.GetSubs). 문제 수를 돌려준다
    private static int CheckAI(AIDef ai)
    {
        List<string> listError = new();
        HashSet<ECoreState> setCore = new();
        foreach (ECoreState type in ai.ListCore)
        {
            if (AICore.TryGetNeeds(type, out _) == false)
                listError.Add($"AI 코어가 아닌 {type}");
            else if (setCore.Add(type) == false)
                listError.Add($"코어 {type} 중복");
        }

        if (setCore.Contains(ai.DefaultCore) == false)
            listError.Add($"기본 코어 {ai.DefaultCore}가 목록에 없음");

        // 피격 뒤 코어는 없어도 된다(Free): 피격 전 코어로 돌아간다
        if (ai.AfterHitCore != ECoreState.Free && setCore.Contains(ai.AfterHitCore) == false)
            listError.Add($"피격 뒤 코어 {ai.AfterHitCore}가 목록에 없음");

        foreach (string error in listError)
            CLogger.Error("DataTools", $"AI 프리셋 {ai.name}: {error}", ai);

        return listError.Count;
    }

    // ID 비어 있음, ID 중복, AI 프리셋 조립을 잡는다. 문제가 없으면 true
    private static bool Validate(List<DataDef> listDef, bool isVerbose)
    {
        int errorCount = 0;

        foreach (DataDef def in listDef.Where(d => string.IsNullOrWhiteSpace(d.ID)))
        {
            CLogger.Error("DataTools", $"ID가 비어 있음: {AssetDatabase.GetAssetPath(def)}", def);
            errorCount++;
        }

        IEnumerable<IGrouping<string, DataDef>> groups = listDef.Where(d => string.IsNullOrWhiteSpace(d.ID) == false)
            .GroupBy(d => d.ID)
            .Where(g => g.Count() > 1);

        foreach (IGrouping<string, DataDef> group in groups)
        {
            string paths = string.Join(", ", group.Select(AssetDatabase.GetAssetPath));

            CLogger.Error("DataTools", $"ID 중복 '{group.Key}': {paths}", group.First());
            errorCount++;
        }

        foreach (AIDef ai in listDef.OfType<AIDef>())
            errorCount += CheckAI(ai);

        if (isVerbose && errorCount == 0)
            CLogger.Log("DataTools", $"검사 통과: {listDef.Count}개");

        return errorCount == 0;
    }
}
