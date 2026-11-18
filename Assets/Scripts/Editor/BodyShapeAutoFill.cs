using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==================================================

// 보스 몸 판정 자동 채움. 모델의 Hitbox_* 뼈마다 캡슐(또는 구) 하나를 MonsterDef 몸 판정 표에 넣는다.
// 메시 점을 가장 가까운 Hitbox 뼈에 나눠 담고, 뼈 Y축으로 투영한 길이와 축까지 거리의 CoverRate 지점으로 잰다.
// 축 방향 길이가 반지름보다 짧으면 구(거리의 CoverRate 지점)
// !!) 기존 몸 판정 표를 통째로 바꾼다. 적용 뒤 기즈모로 보고 손으로 다듬는다
public static class BodyShapeAutoFill
{
    private const string BossMark = "_Boss_";  // MonsterAutoFill과 같은 규칙
    private const float CoverRate = 0.8f;      // 뼈에 담긴 점의 80%를 덮는 반지름
    private const float MinRadius = 0.2f;
    private const float RoundStep = 0.05f;

    private readonly struct Shape
    {
        public readonly EBattlePartType Part;
        public readonly float Radius;
        public readonly float Length;

        public Shape(EBattlePartType part, float radius, float length)
        {
            Part = part;
            Radius = radius;
            Length = length;
        }
    }

    private class Plan
    {
        public MonsterDef Def;
        public readonly List<Shape> ListShape = new();
        public readonly List<string> ListWarning = new();
    }

    // ==================================================

    // 보스만 골라 계획을 보여 주고, 확인을 받으면 적용한다. 적용했으면 true
    public static bool Run(IReadOnlyList<MonsterDef> listDef)
    {
        List<Plan> listPlan = new();
        foreach (MonsterDef def in listDef)
        {
            if (def.name.Contains(BossMark))
                listPlan.Add(MakePlan(def));
        }

        if (listPlan.Count == 0)
        {
            EditorUtility.DisplayDialog("보스 몸 판정", "고른 몬스터 중 보스(이름에 _Boss_)가 없습니다.", "확인");
            return false;
        }

        string report = Describe(listPlan);
        CLogger.Log("BodyShapeAutoFill", report);
        if (EditorUtility.DisplayDialog("보스 몸 판정", report + "\n기존 몸 판정 표를 바꿉니다. Ctrl+Z로 되돌릴 수 있습니다.", "적용", "취소") == false)
            return false;

        Apply(listPlan);
        return true;
    }

    private static Plan MakePlan(MonsterDef def)
    {
        Plan plan = new Plan { Def = def };
        GameObject model = BattleSetup.LoadModel(MonsterPrefabBuilder.GetModelName(def));
        if (model == null)
        {
            plan.ListWarning.Add("모델 없음");
            return plan;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            Dictionary<EBattlePartType, Transform> dicBone = new();
            BodyPartMap.CollectBattleBones(instance.transform, dicBone, def);
            if (dicBone.Count == 0)
            {
                plan.ListWarning.Add("Hitbox_ 뼈 없음");
                return plan;
            }

            List<EBattlePartType> listPart = dicBone.Keys.OrderBy(p => (int)p).ToList();
            List<Vector3> listBonePos = listPart.Select(p => dicBone[p].position).ToList();
            List<Vector3>[] offsets = listPart.Select(_ => new List<Vector3>()).ToArray();
            foreach (Vector3 point in CollectMeshPoints(instance))
            {
                int nearest = 0;
                float nearestSq = float.MaxValue;
                for (int i = 0; i < listBonePos.Count; i++)
                {
                    float distanceSq = (point - listBonePos[i]).sqrMagnitude;
                    if (distanceSq < nearestSq)
                    {
                        nearest = i;
                        nearestSq = distanceSq;
                    }
                }

                offsets[nearest].Add(point - listBonePos[nearest]);
            }

            for (int i = 0; i < listPart.Count; i++)
            {
                if (offsets[i].Count == 0)
                    plan.ListWarning.Add($"{listPart[i]}: 담긴 메시 점이 없어 최소 반지름");

                plan.ListShape.Add(MakeShape(listPart[i], dicBone[listPart[i]].up, offsets[i]));
            }
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }

        return plan;
    }

    // 지금 자세(바인드 포즈)의 메시 점을 월드 좌표로
    private static IEnumerable<Vector3> CollectMeshPoints(GameObject instance)
    {
        foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh baked = new Mesh();
            skinned.BakeMesh(baked);
            Vector3[] vertices = baked.vertices;
            Object.DestroyImmediate(baked);
            foreach (Vector3 vertex in vertices)
            {
                yield return skinned.transform.TransformPoint(vertex);
            }
        }

        foreach (MeshFilter meshFilter in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null)
                continue;

            foreach (Vector3 vertex in meshFilter.sharedMesh.vertices)
            {
                yield return meshFilter.transform.TransformPoint(vertex);
            }
        }
    }

    // 뼈 위치 기준 점들로 캡슐. 축 방향 반길이가 축까지 거리보다 길면 캡슐, 아니면 구
    private static Shape MakeShape(EBattlePartType part, Vector3 axis, List<Vector3> listOffset)
    {
        List<float> listAlong = new();
        List<float> listAcross = new();
        List<float> listDistance = new();
        foreach (Vector3 offset in listOffset)
        {
            float along = Vector3.Dot(offset, axis);
            listAlong.Add(Mathf.Abs(along));
            listAcross.Add((offset - axis * along).magnitude);
            listDistance.Add(offset.magnitude);
        }

        float halfLength = GetCover(listAlong);
        float across = GetCover(listAcross);
        if (halfLength <= across)
            return new Shape(part, RoundUp(GetCover(listDistance)), 0f);

        float radius = RoundUp(across);
        return new Shape(part, radius, Round(Mathf.Max(0f, halfLength - radius) * 2f));
    }

    // 값들의 CoverRate 지점
    private static float GetCover(List<float> listValue)
    {
        if (listValue.Count == 0)
            return 0f;

        listValue.Sort();
        return listValue[Mathf.Min(listValue.Count - 1, (int)(listValue.Count * CoverRate))];
    }

    private static float Round(float value) => Mathf.Round(value / RoundStep) * RoundStep;

    private static float RoundUp(float value) => Mathf.Max(MinRadius, Round(value));

    private static string Describe(List<Plan> listPlan)
    {
        StringBuilder builder = new StringBuilder();
        foreach (Plan plan in listPlan)
        {
            builder.AppendLine($"{plan.Def.name}: " + (plan.ListShape.Count == 0 ? "건너뜀" : string.Join(", ", plan.ListShape.Select(s => s.Length > 0f ? $"{s.Part} r{s.Radius:0.##} L{s.Length:0.##}" : $"{s.Part} r{s.Radius:0.##}"))));
            foreach (string warning in plan.ListWarning)
            {
                builder.AppendLine($"  !! {warning}");
            }
        }

        return builder.ToString();
    }

    private static void Apply(List<Plan> listPlan)
    {
        Undo.SetCurrentGroupName("보스 몸 판정 자동 채움");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (Plan plan in listPlan)
        {
            if (plan.ListShape.Count == 0)
                continue;

            using SerializedObject so = new SerializedObject(plan.Def);
            SerializedProperty listShape = so.FindProperty("m_ListBodyShape");
            listShape.arraySize = plan.ListShape.Count;
            for (int i = 0; i < plan.ListShape.Count; i++)
            {
                SerializedProperty shape = listShape.GetArrayElementAtIndex(i);
                shape.FindPropertyRelative("<Part>k__BackingField").intValue = (int)plan.ListShape[i].Part;
                shape.FindPropertyRelative("<Radius>k__BackingField").floatValue = plan.ListShape[i].Radius;
                shape.FindPropertyRelative("<Length>k__BackingField").floatValue = plan.ListShape[i].Length;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(plan.Def);
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
    }
}
