using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// 애니메이터 다시 만들기. 모델에 클립을 더한 뒤(보스 Skill_04 등) 돌리면 AC_*의 상태를 클립 목록대로 다시 채운다.
// 프리팹·SkillDef·ListSkill은 건드리지 않는다(애니 이름 일괄 정리는 분노 스킬까지 ListSkill에 넣으므로 쓰지 않는다)

public static class AnimatorRebuild
{
    [MenuItem("MushTaker/애니메이터 다시 만들기", false, 31)]
    private static void Run()
    {
        int count = 0;
        foreach (MonsterDef def in DataTools.CollectDefs().OfType<MonsterDef>())
        {
            if (Rebuild(MonsterPrefabBuilder.GetModelName(def)))
                count++;
        }

        if (Rebuild("Player"))
            count++;

        AssetDatabase.SaveAssets();
        CLogger.Log("AnimatorRebuild", $"애니메이터 {count}개를 다시 만들었습니다.");
    }

    private static bool Rebuild(string modelName)
    {
        GameObject model = BattleSetup.LoadModel(modelName);
        if (model == null)
        {
            CLogger.Warning("AnimatorRebuild", $"모델이 없어 건너뜀: {modelName}");
            return false;
        }

        BattleSetup.BuildController(model);
        return true;
    }
}
