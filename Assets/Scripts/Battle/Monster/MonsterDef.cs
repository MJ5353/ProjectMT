using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 몬스터 정의. 몸 수치(UnitDef)에 이동·AI 거리·몸에 꽂는 버섯·드랍을 더한다.
// 새 몬스터은 이 에셋 한 장 + 프리팹으로 늘린다
[CreateAssetMenu(menuName = "MushTaker/Data/Monster", fileName = "Monster_")]
public class MonsterDef : UnitDef
{
    // 몸의 Socket_* 뼈에 꽂는 버섯
    [Serializable]
    public class MushroomSocket
    {
        [field: SerializeField]
        public EMushroomPartType Part { get; private set; } = EMushroomPartType.Head; // 꽂을 자리(프리팹 Socket_* 뼈)

        [field: SerializeField, FormerlySerializedAs("m_Model")]
        public Renderer Model { get; private set; } = null;               // 버섯 모델(Mush_*.fbx)

        [field: SerializeField, FormerlySerializedAs("m_Scale")]
        public float Scale { get; private set; } = 1f;

        [field: SerializeField, FormerlySerializedAs("m_Euler")]
        public Vector3 Euler { get; private set; } = Vector3.zero;        // 버섯이 눕는다면 여기서 돌린다

        // 부위 파괴 ---
        [field: SerializeField]
        public Renderer BrokenModel { get; private set; } = null;         // 깨진 버섯 모델(Mush_*_Broken.fbx). 비면 깨지면 사라진다

        [field: SerializeField]
        public int Durability { get; private set; } = 0;                  // 부위 내구도. 0이면 안 깨지는 장식

        [field: SerializeField]
        public float HitRadius { get; private set; } = 0f;                // 부위 판정 구 반지름(m). 0이면 판정 없음

        [field: SerializeField]
        public float BodyDamageScale { get; private set; } = 1f;          // 이 부위를 때릴 때 본체에 들어가는 피해 배율(구름 갑옷은 1 미만). 0 이하면 1

        // !!) 기존 에셋에 새로 생긴 칸은 0으로 들어온다. 0을 1로 봐야 본체 피해가 사라지지 않는다
        public float BodyDamageRate => BodyDamageScale > 0f ? BodyDamageScale : 1f;

        [field: SerializeField]
        public float RegenTime { get; private set; } = 0f;                // 깨진 뒤 다시 돋기까지(초). 0이면 재생 안 함

        [field: SerializeField]
        public MushroomPickup Drop { get; private set; } = null;          // 깨질 때 확정으로 떨구는 버섯

        public bool IsBreakable => Durability > 0;

        // ------------------------------------------------

        public MushroomSocket(EMushroomPartType part, Renderer model, float scale)
        {
            Part = part;
            Model = model;
            Scale = scale;
        }
    }

    // 죽을 때 떨구는 버섯
    [Serializable]
    public class DropEntry
    {
        [field: SerializeField, FormerlySerializedAs("m_Pickup")]
        public MushroomPickup Pickup { get; private set; } = null;

        [field: SerializeField, FormerlySerializedAs("m_Min")]
        public int Min { get; private set; } = 1;                      // 최소 개수

        [field: SerializeField, FormerlySerializedAs("m_Max")]
        public int Max { get; private set; } = 1;                      // 최대 개수

        [field: SerializeField, Range(0f, 1f), FormerlySerializedAs("m_Chance")]
        public float Chance { get; private set; } = 1f; // 떨굴 확률

        // ------------------------------------------------

        public DropEntry(MushroomPickup pickup, int min, int max, float chance)
        {
            Pickup = pickup;
            Min = min;
            Max = max;
            Chance = chance;
        }
    }

    // 이동 ---
    [field: SerializeField, FormerlySerializedAs("m_MoveSpeed")]
    public float MoveSpeed { get; private set; } = 3f;       // 추적 속도

    [field: SerializeField, FormerlySerializedAs("m_WanderSpeed")]
    public float WanderSpeed { get; private set; } = 1.2f;   // 배회 속도

    // AI ---
    [field: SerializeField, FormerlySerializedAs("m_SightRange")]
    public float SightRange { get; private set; } = 7f;      // 플레이어를 알아채는 거리

    [field: SerializeField, FormerlySerializedAs("m_LoseRange")]
    public float LoseRange { get; private set; } = 12f;      // 추적을 포기하는 거리

    [field: SerializeField, FormerlySerializedAs("m_WanderRadius")]
    public float WanderRadius { get; private set; } = 4f;    // 처음 자리에서 배회하는 반경

    [field: SerializeField, FormerlySerializedAs("m_AttackRange")]
    public float AttackRange { get; private set; } = 1.4f;   // 추적을 멈추고 마주 보는 거리. 공격 선택은 AttackSpec.MinRange·MaxRange

    [field: SerializeField, FormerlySerializedAs("m_AttackCooldown")]
    public float AttackCooldown { get; private set; } = 1.2f; // 공격 하나가 끝난 뒤 다음 공격까지 공용 쿨(초)

    [field: SerializeField, FormerlySerializedAs("m_FleeTime")]
    public float FleeTime { get; private set; } = 0f;        // 맞은 뒤 도망치는 시간. 0이면 바로 반격

    [field: SerializeField, FormerlySerializedAs("m_FleeSpeed")]
    public float FleeSpeed { get; private set; } = 4f;

    // 몸(프리팹)---  MonsterPrefabBuilder가 프리팹을 구울 때 읽고, 구운 프리팹을 Prefab에 채운다
    [field: SerializeField]
    public MonsterController Prefab { get; private set; } = null;

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
    [SerializeField] private List<MushroomSocket> m_ListMushroom = new();

    [SerializeField] private List<DropEntry> m_ListDrop = new();

    public IReadOnlyList<MushroomSocket> ListMushroom => m_ListMushroom;
    public IReadOnlyList<DropEntry> ListDrop => m_ListDrop;

    // ------------------------------------------------

#if UNITY_EDITOR
    // BattleSetup 메뉴가 만들 때만 쓴다
    public void EditorSetup(AttackSpec attack, float fleeTime, List<DropEntry> listDrop)
    {
        EditorSetAttack(attack);
        FleeTime = fleeTime;
        m_ListDrop = listDrop;
    }

    public void EditorSetPrefab(MonsterController prefab) => Prefab = prefab;

    public void EditorSetMushrooms(List<MushroomSocket> listMushroom)
    {
        m_ListMushroom = listMushroom;
    }
#endif
}
