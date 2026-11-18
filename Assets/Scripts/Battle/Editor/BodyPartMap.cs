using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

// ==================================================

// 모델 뼈 이름 → 부위. Hitbox_* → EBattlePartType. Socket_*은 가장 가까운 Hitbox 뼈의 부위로 들어간다.
// 프리팹을 구울 때와 Def를 만들 때 여기만 본다. 새 모델에 표에 없는 뼈가 생기면 여기에 한 줄 추가한다
// (Hitbox_Custom_01처럼 enum 이름을 그대로 써도 된다)

public static class BodyPartMap
{
    public const string HitboxPrefix = "Hitbox_";
    public const string SocketPrefix = "Socket_";

    // Hitbox_ 뒤 이름 → 전투 부위. 네발은 앞다리 = Arm, 뒷다리 = Leg
    private static readonly Dictionary<string, EBattlePartType> s_DicBattlePart = new()
    {
        { "Body", EBattlePartType.Body },
        { "Trunk", EBattlePartType.Body },          // 나무 보스 몸통
        { "Head", EBattlePartType.Head },
        { "Face", EBattlePartType.Head },           // 나무 보스 얼굴
        { "Neck", EBattlePartType.Custom_01 },      // 목(공룡·곰 보스)
        { "Tail", EBattlePartType.Tail },
        { "ArmL", EBattlePartType.LeftArm },
        { "ArmR", EBattlePartType.RightArm },
        { "LegFL", EBattlePartType.LeftArm },
        { "LegFR", EBattlePartType.RightArm },
        { "PawFL", EBattlePartType.LeftArm },
        { "PawFR", EBattlePartType.RightArm },
        { "RootFL", EBattlePartType.Custom_02 },    // 나무 보스 앞 뿌리(팔이 따로 있음)
        { "RootFR", EBattlePartType.Custom_03 },
        { "LegBL", EBattlePartType.LeftLeg },
        { "LegBR", EBattlePartType.RightLeg },
        { "PawBL", EBattlePartType.LeftLeg },
        { "PawBR", EBattlePartType.RightLeg },
        { "RootBL", EBattlePartType.LeftLeg },      // 나무 보스 뒤 뿌리
        { "RootBR", EBattlePartType.RightLeg },
    };

    // ------------------------------------------------

    // 모델 아래 Hitbox_* 뼈를 전투 부위별로 채운다
    public static void CollectBattleBones(Transform root, IDictionary<EBattlePartType, Transform> dicBone, Object context)
    {
        Collect(root, HitboxPrefix, s_DicBattlePart, dicBone, context);
    }

    // 부위마다 판정 뼈와 소켓 목록. Socket_* 뼈는 가장 가까운 Hitbox_* 뼈의 부위에 넣는다(이름순)
    public static List<UnitProperty.BonePart> CollectBoneParts(Transform root, Object context)
    {
        Dictionary<EBattlePartType, Transform> dicBone = new();
        CollectBattleBones(root, dicBone, context);

        List<UnitProperty.BonePart> listBonePart = new();
        foreach (var pair in dicBone)
        {
            listBonePart.Add(new UnitProperty.BonePart(pair.Key, pair.Value));
        }

        IEnumerable<Transform> sockets = root.GetComponentsInChildren<Transform>(true)
            .Where(bone => bone.name.StartsWith(SocketPrefix))
            .OrderBy(bone => bone.name, StringComparer.Ordinal);
        foreach (Transform socket in sockets)
        {
            UnitProperty.BonePart nearest = listBonePart
                .OrderBy(bonePart => (bonePart.Bone.position - socket.position).sqrMagnitude)
                .FirstOrDefault();
            if (nearest == null)
            {
                CLogger.Warning("BodyPartMap", $"Hitbox 뼈가 없어 소켓을 넣을 부위가 없음: {socket.name} ({root.name})", context);
                continue;
            }

            nearest.ListSocket.Add(socket);
        }

        return listBonePart;
    }

    // ==================================================

    // 접두사를 뗀 이름으로 부위를 찾는다. 표에 없으면 enum 이름(Custom_01 등)으로도 본다
    private static bool TryGetPart<TPart>(string boneName, string prefix, Dictionary<string, TPart> dicPart, out TPart part) where TPart : struct, Enum
    {
        part = default;
        if (boneName.StartsWith(prefix) == false)
            return false;

        string key = boneName.Substring(prefix.Length);
        if (dicPart.TryGetValue(key, out part))
            return true;

        return Enum.TryParse(key, out part) && Convert.ToInt32(part) != 0;
    }

    // 표에 없거나 같은 부위가 겹치면 경고하고 건너뛴다
    private static void Collect<TPart>(Transform root, string prefix, Dictionary<string, TPart> dicPart, IDictionary<TPart, Transform> dicBone, Object context) where TPart : struct, Enum
    {
        dicBone.Clear();
        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name.StartsWith(prefix) == false)
                continue;

            if (TryGetPart(bone.name, prefix, dicPart, out TPart part) == false)
            {
                CLogger.Warning("BodyPartMap", $"부위 표에 없는 뼈: {bone.name} ({root.name}). BodyPartMap에 추가하세요", context);
                continue;
            }

            if (dicBone.ContainsKey(part))
            {
                CLogger.Warning("BodyPartMap", $"같은 부위({part})에 뼈가 둘: {bone.name} ({root.name})", context);
                continue;
            }

            dicBone.Add(part, bone);
        }
    }
}
