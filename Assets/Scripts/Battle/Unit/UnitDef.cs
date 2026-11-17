using UnityEngine;

// 플레이어와 몬스터이 같이 쓰는 몸 수치. 몬스터은 MonsterDef, 플레이어는 PlayerDef가 이어받는다
public abstract class UnitDef : DataDef
{
    // 몸 ---
    [SerializeField] private int m_MaxHp = 3;                    // 최대 체력
    [SerializeField] private float m_KnockbackScale = 1f;        // 넉백 배율. 1이면 그대로, 0이면 안 밀림
    [SerializeField] private float m_HitStunTime = 0.35f;        // 맞고 굳는 시간(초)
    [SerializeField] private float m_InvincibleTime = 0f;        // 맞은 뒤 무적 시간(초)
    [SerializeField] private Color m_ChunkColor = new(0.85f, 0.75f, 0.6f); // 맞을 때 튀는 복셀 조각 색

    // 기본 공격 ---
    [SerializeField] private AttackSpec m_Attack = new();

    public int MaxHp => m_MaxHp;
    public float KnockbackScale => m_KnockbackScale;
    public float HitStunTime => m_HitStunTime;
    public float InvincibleTime => m_InvincibleTime;
    public Color ChunkColor => m_ChunkColor;
    public AttackSpec Attack => m_Attack;

    // ------------------------------------------------

#if UNITY_EDITOR
    // BattleSetup 메뉴가 처음 만들 때만 쓴다
    public void EditorSetAttack(AttackSpec attack)
    {
        m_Attack = attack;
    }
#endif
}
