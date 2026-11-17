using System;
using System.Collections.Generic;
using UnityEngine;

// 몬스터 정의. 몸 수치(UnitDef)에 이동·AI 거리·몸에 꽂는 버섯·드랍을 더한다.
// 새 몬스터은 이 에셋 한 장 + 프리팹으로 늘린다
[CreateAssetMenu(menuName = "MushTaker/Data/Monster", fileName = "Monster_")]
public class MonsterDef : UnitDef
{
    // 몸의 Socket_* 뼈에 꽂는 버섯
    [Serializable]
    public class MushroomSocket
    {
        [SerializeField] private string m_SocketName = "Socket_Head";   // 꽂을 뼈 이름
        [SerializeField] private Renderer m_Model = null;               // 버섯 모델(Mush_*.fbx)
        [SerializeField] private float m_Scale = 1f;
        [SerializeField] private Vector3 m_Euler = Vector3.zero;        // 버섯이 눕는다면 여기서 돌린다

        public string SocketName => m_SocketName;
        public Renderer Model => m_Model;
        public float Scale => m_Scale;
        public Vector3 Euler => m_Euler;

        public MushroomSocket(string socketName, Renderer model, float scale)
        {
            m_SocketName = socketName;
            m_Model = model;
            m_Scale = scale;
        }
    }

    // 죽을 때 떨구는 버섯
    [Serializable]
    public class DropEntry
    {
        [SerializeField] private MushroomPickup m_Pickup = null;
        [SerializeField] private int m_Min = 1;                      // 최소 개수
        [SerializeField] private int m_Max = 1;                      // 최대 개수
        [SerializeField, Range(0f, 1f)] private float m_Chance = 1f; // 떨굴 확률

        public MushroomPickup Pickup => m_Pickup;
        public int Min => m_Min;
        public int Max => m_Max;
        public float Chance => m_Chance;

        public DropEntry(MushroomPickup pickup, int min, int max, float chance)
        {
            m_Pickup = pickup;
            m_Min = min;
            m_Max = max;
            m_Chance = chance;
        }
    }

    // 이동---
    [SerializeField] private float m_MoveSpeed = 3f;       // 추적 속도
    [SerializeField] private float m_WanderSpeed = 1.2f;   // 배회 속도

    // AI---
    [SerializeField] private float m_SightRange = 7f;      // 플레이어를 알아채는 거리
    [SerializeField] private float m_LoseRange = 12f;      // 추적을 포기하는 거리
    [SerializeField] private float m_WanderRadius = 4f;    // 처음 자리에서 배회하는 반경
    [SerializeField] private float m_AttackRange = 1.4f;   // 이 거리 안이면 공격을 시작
    [SerializeField] private float m_WindupTime = 0.45f;   // 공격 전에 움츠리는 시간(예고)
    [SerializeField] private float m_AttackCooldown = 1.2f;
    [SerializeField] private float m_FleeTime = 0f;        // 맞은 뒤 도망치는 시간. 0이면 바로 반격
    [SerializeField] private float m_FleeSpeed = 4f;

    // 버섯·드랍---
    [SerializeField] private List<MushroomSocket> m_ListMushroom = new();
    [SerializeField] private List<DropEntry> m_ListDrop = new();

    public float MoveSpeed => m_MoveSpeed;
    public float WanderSpeed => m_WanderSpeed;
    public float SightRange => m_SightRange;
    public float LoseRange => m_LoseRange;
    public float WanderRadius => m_WanderRadius;
    public float AttackRange => m_AttackRange;
    public float WindupTime => m_WindupTime;
    public float AttackCooldown => m_AttackCooldown;
    public float FleeTime => m_FleeTime;
    public float FleeSpeed => m_FleeSpeed;
    public IReadOnlyList<MushroomSocket> ListMushroom => m_ListMushroom;
    public IReadOnlyList<DropEntry> ListDrop => m_ListDrop;

#if UNITY_EDITOR
    // BattleSetup 메뉴가 만들 때만 쓴다
    public void EditorSetup(AttackSpec attack, float fleeTime, List<DropEntry> listDrop)
    {
        EditorSetAttack(attack);
        m_FleeTime = fleeTime;
        m_ListDrop = listDrop;
    }

    public void EditorSetMushrooms(List<MushroomSocket> listMushroom)
    {
        m_ListMushroom = listMushroom;
    }
#endif
}
