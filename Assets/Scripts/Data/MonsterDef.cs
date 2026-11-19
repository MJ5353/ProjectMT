using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 몬스터 정의. 몸 수치(UnitDef, 이동 속도·AI 프리셋 포함)에 프리팹·몸 판정·몸에 꽂는 버섯·드랍을 더한다.
// 새 몬스터은 이 에셋 한 장 + 프리팹으로 늘린다

[CreateAssetMenu(menuName = "MushTaker/Data/Monster", fileName = "Monster_")]
public class MonsterDef : UnitDef
{
    // 몸 부위 하나에 나는 버섯. 스폰할 때마다 그 부위 소켓(UnitProperty.BonePart) 중 몇 개를 골라 꽂는다.
    // 내구도는 부위 단위라 꽂힌 버섯이 같이 깨진다. BattlePart None은 몸 어디든 나는 장식(소켓이 남는 곳)
    [Serializable]
    public class PartEntry
    {
        [field: SerializeField]
        public EBattlePartType BattlePart { get; private set; } = EBattlePartType.None; // 이 몸 부위(ListBodyShape)가 맞으면 내구도가 깎임

        // 버섯 ---
        [field: SerializeField]
        public Renderer Model { get; private set; } = null;               // 버섯 모델(Mush_*.fbx)

        [field: SerializeField]
        public Renderer BrokenModel { get; private set; } = null;         // 깨진 버섯 모델(Mush_*_Broken.fbx). 비면 깨지면 사라진다

        [field: SerializeField]
        public MushroomPickup Drop { get; private set; } = null;          // 깨질 때 버섯 하나마다 확정으로 떨구는 픽업

        [field: SerializeField]
        public int CountMin { get; private set; } = 1;                    // 스폰 때 꽂을 개수(소켓 수를 넘으면 소켓 수까지)

        [field: SerializeField]
        public int CountMax { get; private set; } = 1;

        [field: SerializeField]
        public Vector2 ScaleRange { get; private set; } = Vector2.one;    // 버섯 크기 랜덤 범위

        [field: SerializeField]
        public float TiltMax { get; private set; } = 0f;                  // 축마다 랜덤으로 기울이는 최대 각도

        // 부위 파괴 ---
        [field: SerializeField]
        public int Durability { get; private set; } = 0;                  // 부위 내구도. 0이면 안 깨지는 장식

        [field: SerializeField]
        public float BodyDamageScale { get; private set; } = 1f;          // 이 부위를 때릴 때 본체에 들어가는 피해 배율(구름 갑옷은 1 미만). 0 이하면 1

        [field: SerializeField]
        public float RegenTime { get; private set; } = 0f;                // 깨진 뒤 다시 돋기까지(초). 0이면 재생 안 함

        // get ---
        // !!) 기존 에셋에 새로 생긴 칸은 0으로 들어온다. 0을 1로 봐야 본체 피해가 사라지지 않는다
        public float BodyDamageRate => BodyDamageScale > 0f ? BodyDamageScale : 1f;

        public bool IsBreakable => Durability > 0 && BattlePart != EBattlePartType.None;

        // ------------------------------------------------

#if UNITY_EDITOR
        public PartEntry(EBattlePartType battlePart, Renderer model, Renderer brokenModel, MushroomPickup drop, int countMin, int countMax, Vector2 scaleRange, float tiltMax, int durability, float bodyDamageScale, float regenTime)
        {
            BattlePart = battlePart;
            Model = model;
            BrokenModel = brokenModel;
            Drop = drop;
            CountMin = countMin;
            CountMax = countMax;
            ScaleRange = scaleRange;
            TiltMax = tiltMax;
            Durability = durability;
            BodyDamageScale = bodyDamageScale;
            RegenTime = regenTime;
        }
#endif
    }

    // 죽을 때 떨구는 버섯
    [Serializable]
    public class DropEntry
    {
        [field: SerializeField, FormerlySerializedAs("m_Pickup")]
        public MushroomPickup Pickup { get; private set; } = null;

        [field: SerializeField, FormerlySerializedAs("m_Min")]
        public int Min { get; private set; } // 최소 개수

        [field: SerializeField, FormerlySerializedAs("m_Max")]
        public int Max { get; private set; } // 최대 개수

        [field: SerializeField, Range(0f, 1f), FormerlySerializedAs("m_Chance")]
        public float Chance { get; private set; } // 떨굴 확률

        // ------------------------------------------------

        public DropEntry(MushroomPickup pickup, int min, int max, float chance)
        {
            Pickup = pickup;
            Min = min;
            Max = max;
            Chance = chance;
        }
    }

    // ------------------------------------------------
    
    // 몸(프리팹)---  MonsterPrefabBuilder가 프리팹을 구울 때 읽고, 구운 프리팹을 Prefab에 채운다
    [field: SerializeField]
    public UnitController Prefab { get; private set; } = null;

    [field: SerializeField, FormerlySerializedAs("m_ModelName")]
    public string ModelName { get; private set; } = null;    // Art/Voxel/Models 아래 FBX 이름. 비면 에셋 이름(Monster_Xxx)의 Xxx

    [field: SerializeField, FormerlySerializedAs("m_BodyHeight")]
    public float BodyHeight { get; private set; } = 0f;      // 캡슐 높이. 0이면 모델 크기로 자동 계산해 채운다

    [field: SerializeField, FormerlySerializedAs("m_BodyRadius")]
    public float BodyRadius { get; private set; } = 0f;      // 캡슐 반지름. 0이면 모델 크기로 자동 계산해 채운다

    // 몸 판정 --- 비면 CharacterController 캡슐로 맞는다. 큰 몹(보스)은 뼈마다 구·캡슐을 둔다
    [SerializeField] private List<HitShapeDef> m_ListBodyShape = new();

    public IReadOnlyList<HitShapeDef> ListBodyShape => m_ListBodyShape;

    // 버섯·드랍 ---
    [SerializeField] private List<PartEntry> m_ListPart = new();

    [SerializeField] private List<DropEntry> m_ListDrop = new();

    // get ---
    public IReadOnlyList<PartEntry> ListPart => m_ListPart;
    public IReadOnlyList<DropEntry> ListDrop => m_ListDrop;

    // ------------------------------------------------

    // 버섯 부위·판정 모양·사망 보상을 붙인다. 판단은 기본(AIBrain, 프리셋 AI)
    public override void SetFeatures(UnitController unit)
    {
        // 버섯 부위(스폰 때 소켓을 골라 꽂는다)
        unit.SetPartFeature(new PartFeature(unit, ListPart));

        // 뼈 판정 모양. 부위 번호를 쓰므로 부위 다음
        unit.HitF.SetShapes(ListBodyShape);

        // 죽으면 버섯 드롭·처치 알림
        unit.AddFeature(new DropFeature(unit, this));
    }

#if UNITY_EDITOR
    private HitShapeDef FindBodyShape(EBattlePartType battlePart)
    {
        foreach (HitShapeDef shapeDef in ListBodyShape)
        {
            if (shapeDef.Part == battlePart)
                return shapeDef;
        }

        return null;
    }

    // 씬 뷰에 몸 판정 모양(구·캡슐)과 부위 판정 구를 그린다. 선이 뼈 축 방향
    public override void DrawGizmos(UnitController unit)
    {
        DrawPartGizmos(unit);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        foreach (HitShapeDef shapeDef in ListBodyShape)
        {
            Transform bone = unit.GetBattleBone(shapeDef.Part);
            if (bone == null)
                continue;

            new HitShape(bone, shapeDef, -1).GetSegment(out Vector3 start, out Vector3 end);
            Gizmos.DrawWireSphere(start, shapeDef.Radius);
            Gizmos.DrawWireSphere(end, shapeDef.Radius);
            Gizmos.DrawLine(start, end);
        }
    }

    // 깨지는 부위의 몸 판정 모양 위에 상태색(초록 멀쩡, 빨강 깨짐)과 남은 내구도를 그린다
    private void DrawPartGizmos(UnitController unit)
    {
        for (int i = 0; i < m_ListPart.Count; i++)
        {
            PartEntry entry = m_ListPart[i];
            if (entry.IsBreakable == false)
                continue;

            Transform bone = unit.GetBattleBone(entry.BattlePart);
            HitShapeDef shapeDef = FindBodyShape(entry.BattlePart);
            if (bone == null || shapeDef == null)
                continue;

            PartFeature.Part part = unit.PartF?.GetPart(i);
            Gizmos.color = part != null && part.IsBroken ? Color.red : Color.green;
            new HitShape(bone, shapeDef, i).GetSegment(out Vector3 start, out Vector3 end);
            Gizmos.DrawWireSphere(start, shapeDef.Radius);
            Gizmos.DrawWireSphere(end, shapeDef.Radius);
            Gizmos.DrawLine(start, end);

            string durability = part != null ? $"{part.Durability}/{entry.Durability}" : $"{entry.Durability}";
            UnityEditor.Handles.Label(end + Vector3.up * shapeDef.Radius, $"{i}:{entry.BattlePart} {durability}");
        }
    }
#endif

#if UNITY_EDITOR
    // BattleSetup 메뉴가 만들 때만 쓴다
    public void EditorSetup(List<DropEntry> listDrop)
    {
        m_ListDrop = listDrop;
    }

    public void EditorSetPrefab(UnitController prefab) => Prefab = prefab;

    public void EditorSetParts(List<PartEntry> listPart)
    {
        m_ListPart = listPart;
    }
#endif
}
