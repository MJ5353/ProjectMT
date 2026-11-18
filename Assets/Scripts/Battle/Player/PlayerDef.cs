using UnityEngine;
using UnityEngine.Serialization;

// 채집꾼 수치. 몸 수치(UnitDef)에 이동·공격 보조·대시를 더한다
[CreateAssetMenu(menuName = "MushTaker/Data/Player", fileName = "Player")]
public class PlayerDef : UnitDef
{
    [field: SerializeField, FormerlySerializedAs("m_Prefab")]
    public PlayerController Prefab { get; private set; } = null;

    // 이동 ---
    [field: SerializeField, FormerlySerializedAs("m_MoveSpeed")]
    public float MoveSpeed { get; private set; } = 5f;

    [field: SerializeField, FormerlySerializedAs("m_RunThreshold")]
    public float RunThreshold { get; private set; } = 0.6f;    // 입력 세기가 이 값 이상이면 Run 애니

    // 공격 보조 ---
    [field: SerializeField, FormerlySerializedAs("m_AutoAimRange")]
    public float AutoAimRange { get; private set; } = 3.5f;    // 이 거리 안의 몬스터 쪽으로 자동 회전

    [field: SerializeField, FormerlySerializedAs("m_CancelDelay")]
    public float CancelDelay { get; private set; } = 0.12f;    // 판정 뒤 이 시간이 지나면 다음 공격으로 끊을 수 있음

    // 대시 ---
    [field: SerializeField, FormerlySerializedAs("m_DashSpeed")]
    public float DashSpeed { get; private set; } = 16f;

    [field: SerializeField, FormerlySerializedAs("m_DashTime")]
    public float DashTime { get; private set; } = 0.18f;       // 대시 지속 시간(초)

    [field: SerializeField, FormerlySerializedAs("m_DashCooldown")]
    public float DashCooldown { get; private set; } = 0.5f;    // 대시가 끝난 뒤 다시 쓰기까지

    [field: SerializeField, FormerlySerializedAs("m_IsDashInvincible")]
    public bool IsDashInvincible { get; private set; } = true; // 대시 중 무적

    [field: SerializeField, FormerlySerializedAs("m_DashAnimSpeed")]
    public float DashAnimSpeed { get; private set; } = 1.6f;   // 대시 중 Run 애니 배속

    public AttackSpec UpAttack => GetBasicAttack(1); // 두 번째 기본 공격: 위로 치기(높은 부위용). 없으면 null

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetPrefab(PlayerController prefab) => Prefab = prefab;
#endif
}
