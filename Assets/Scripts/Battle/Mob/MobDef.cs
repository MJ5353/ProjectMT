using System;
using System.Collections.Generic;
using UnityEngine;

// 몹 정의. 체력·속도·AI 거리·공격 수치·몸에 꽂는 버섯·드랍을 한 장에 둔다.
// 새 몹은 이 에셋 한 장 + 프리팹으로 늘린다
[CreateAssetMenu(menuName = "MushTaker/Data/Mob", fileName = "Mob_")]
public class MobDef : DataDef
{
    // 몸의 Socket_* 뼈에 꽂는 버섯
    [Serializable]
    public class MushroomSocket
    {
        [SerializeField] private string m_SocketName = "Socket_Head";
        [SerializeField] private GameObject m_Prefab = null;
        [SerializeField] private float m_Scale = 1f;
        [SerializeField, Tooltip("버섯이 눕는다면 여기서 돌린다")] private Vector3 m_Euler = Vector3.zero;

        public string SocketName => m_SocketName;
        public GameObject Prefab => m_Prefab;
        public float Scale => m_Scale;
        public Vector3 Euler => m_Euler;

        public MushroomSocket(string socketName, GameObject prefab, float scale)
        {
            m_SocketName = socketName;
            m_Prefab = prefab;
            m_Scale = scale;
        }
    }

    [Serializable]
    public class DropEntry
    {
        [SerializeField] private MushroomPickup m_Pickup = null;
        [SerializeField] private int m_Min = 1;
        [SerializeField] private int m_Max = 1;
        [SerializeField, Range(0f, 1f)] private float m_Chance = 1f;

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

    [Header("기본")]
    [SerializeField] private int m_MaxHp = 3;
    [SerializeField] private float m_MoveSpeed = 3f;
    [SerializeField] private float m_WanderSpeed = 1.2f;
    [SerializeField, Tooltip("1이면 그대로, 0이면 안 밀림")] private float m_KnockbackScale = 1f;

    [Header("AI")]
    [SerializeField] private float m_SightRange = 7f;
    [SerializeField, Tooltip("추적을 포기하는 거리")] private float m_LoseRange = 12f;
    [SerializeField] private float m_WanderRadius = 4f;
    [SerializeField] private float m_AttackRange = 1.4f;
    [SerializeField, Tooltip("공격 전에 움츠리는 시간")] private float m_WindupTime = 0.45f;
    [SerializeField] private float m_AttackCooldown = 1.2f;
    [SerializeField, Tooltip("맞고 굳는 시간")] private float m_HitStunTime = 0.35f;
    [SerializeField, Tooltip("맞은 뒤 도망치는 시간. 0이면 바로 반격")] private float m_FleeTime = 0f;
    [SerializeField] private float m_FleeSpeed = 4f;

    [Header("공격")]
    [SerializeField] private AttackSpec m_Attack = new();

    [Header("버섯·드랍")]
    [SerializeField] private List<MushroomSocket> m_ListMushroom = new();
    [SerializeField] private List<DropEntry> m_ListDrop = new();

    public int MaxHp => m_MaxHp;
    public float MoveSpeed => m_MoveSpeed;
    public float WanderSpeed => m_WanderSpeed;
    public float KnockbackScale => m_KnockbackScale;
    public float SightRange => m_SightRange;
    public float LoseRange => m_LoseRange;
    public float WanderRadius => m_WanderRadius;
    public float AttackRange => m_AttackRange;
    public float WindupTime => m_WindupTime;
    public float AttackCooldown => m_AttackCooldown;
    public float HitStunTime => m_HitStunTime;
    public float FleeTime => m_FleeTime;
    public float FleeSpeed => m_FleeSpeed;
    public AttackSpec Attack => m_Attack;
    public IReadOnlyList<MushroomSocket> ListMushroom => m_ListMushroom;
    public IReadOnlyList<DropEntry> ListDrop => m_ListDrop;

#if UNITY_EDITOR
    // BattleSetup 메뉴가 처음 만들 때만 쓴다
    public void EditorSetup(AttackSpec attack, float fleeTime, List<MushroomSocket> listMushroom, List<DropEntry> listDrop)
    {
        m_Attack = attack;
        m_FleeTime = fleeTime;
        m_ListMushroom = listMushroom;
        m_ListDrop = listDrop;
    }
#endif
}
