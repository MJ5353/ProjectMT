using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// ==================================================

// 모델 뼈 이름 → 부위. Hitbox_* → EBattlePartType, Socket_* → EMushroomPartType.
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

    // Socket_ 뒤 이름 → 버섯 자리
    private static readonly Dictionary<string, EMushroomPartType> s_DicMushroomPart = new()
    {
        { "Head", EMushroomPartType.Head },
        { "Crown", EMushroomPartType.Head },        // 나무 보스 정수리
        { "Back", EMushroomPartType.Back_1 },
        { "Back1", EMushroomPartType.Back_1 },
        { "Back2", EMushroomPartType.Back_2 },
        { "Back3", EMushroomPartType.Back_3 },
        { "Hump", EMushroomPartType.Back_2 },       // 곰 보스 혹
        { "Rump", EMushroomPartType.Back_3 },       // 곰 보스 엉덩이 위
        { "SideL", EMushroomPartType.LeftSide },
        { "SideR", EMushroomPartType.RightSide },
        { "TrunkL", EMushroomPartType.LeftSide },   // 나무 보스 몸통 옆
        { "TrunkR", EMushroomPartType.RightSide },
        { "ShoulderL", EMushroomPartType.LeftShoulder },
        { "ShoulderR", EMushroomPartType.RightShoulder },
        { "ForearmL", EMushroomPartType.LeftArm },
        { "ForearmR", EMushroomPartType.RightArm },
        { "ForelegL", EMushroomPartType.LeftArm },
        { "ForelegR", EMushroomPartType.RightArm },
        { "Tail", EMushroomPartType.Tail },
        { "Tail1", EMushroomPartType.Tail },
        { "TailTip", EMushroomPartType.TailTip },
        { "ThighL", EMushroomPartType.Custom_01 },  // 곰 보스 허벅지
        { "ThighR", EMushroomPartType.Custom_02 },
        { "FrillL", EMushroomPartType.Custom_01 },  // 트리케라 프릴
        { "FrillR", EMushroomPartType.Custom_02 },
        { "RootBL", EMushroomPartType.Custom_01 },  // 나무 보스 뒤 뿌리
    };

    // ------------------------------------------------

    public static bool TryGetMushroomPart(string boneName, out EMushroomPartType part)
    {
        return TryGetPart(boneName, SocketPrefix, s_DicMushroomPart, out part);
    }

    // 모델 아래 Hitbox_* 뼈를 전투 부위별로 채운다
    public static void CollectBattleBones(Transform root, IDictionary<EBattlePartType, Transform> dicBone, Object context)
    {
        Collect(root, HitboxPrefix, s_DicBattlePart, dicBone, context);
    }

    // 모델 아래 Socket_* 뼈를 버섯 자리별로 채운다
    public static void CollectMushroomBones(Transform root, IDictionary<EMushroomPartType, Transform> dicBone, Object context)
    {
        Collect(root, SocketPrefix, s_DicMushroomPart, dicBone, context);
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
