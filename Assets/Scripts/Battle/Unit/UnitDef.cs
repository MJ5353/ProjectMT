using UnityEngine;
using UnityEngine.Serialization;

// 플레이어와 몬스터이 같이 쓰는 몸 수치. 몬스터은 MonsterDef, 플레이어는 PlayerDef가 이어받는다
public abstract class UnitDef : DataDef
{
    // 몸 ---
    [field: SerializeField, FormerlySerializedAs("m_MaxHp")]
    public int MaxHp { get; private set; } = 3;                    // 최대 체력

    [field: SerializeField, FormerlySerializedAs("m_KnockbackScale")]
    public float KnockbackScale { get; private set; } = 1f;        // 넉백 배율. 1이면 그대로, 0이면 안 밀림

    [field: SerializeField, FormerlySerializedAs("m_HitStunTime")]
    public float HitStunTime { get; private set; } = 0.35f;        // 맞고 굳는 시간(초)

    [field: SerializeField, FormerlySerializedAs("m_InvincibleTime")]
    public float InvincibleTime { get; private set; } = 0f;        // 맞은 뒤 무적 시간(초)

    [field: SerializeField, FormerlySerializedAs("m_ChunkColor")]
    public Color ChunkColor { get; private set; } = new(0.85f, 0.75f, 0.6f); // 맞을 때 튀는 복셀 조각 색

    // 기본 공격 ---
    [field: SerializeField, FormerlySerializedAs("m_Attack")]
    public AttackSpec Attack { get; private set; } = new();

    // ------------------------------------------------

#if UNITY_EDITOR
    // BattleSetup 메뉴가 처음 만들 때만 쓴다
    public void EditorSetAttack(AttackSpec attack)
    {
        Attack = attack;
    }
#endif
}
