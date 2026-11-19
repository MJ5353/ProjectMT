using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 채집꾼 수치. 몸 수치(UnitDef)에 이동·공격 보조·대시를 더한다
[CreateAssetMenu(menuName = "MushTaker/Data/Player", fileName = "Player")]
public class PlayerDef : UnitDef
{
    [field: SerializeField, FormerlySerializedAs("m_Prefab")]
    public UnitController Prefab { get; private set; } = null;

    // 이동 --- 속도는 UnitDef.MoveSpeed
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

    // 콤보 ---
    // 연타 순서는 첫 타의 AttackSpec.Next로 이어진다(좌 → 우 → 막타). 막타(Next 없음) 다음은 다시 첫 타
    [field: SerializeField]
    public SkillDef ComboStart { get; private set; } = null;       // 콤보 첫 타. 비면 기본 공격 하나

    [field: SerializeField]
    public float ComboResetTime { get; private set; } = 0.4f;      // [mj] todo) 임시값. 공격이 끝난 뒤 이 시간 안에 누르면 다음 타, 지나면 첫 타부터

    [field: SerializeField]
    public SkillDef RunAttack { get; private set; } = null;        // 달리는 중 공격(달리다 베기). 비면 콤보

    [field: SerializeField]
    public SkillDef HoldAttack { get; private set; } = null;       // 공격 버튼을 길게 누르면(회오리). 비면 없음

    [field: SerializeField]
    public float HoldTime { get; private set; } = 0.35f;           // [mj] todo) 임시값. 이만큼 누르고 있으면 HoldAttack

    public AttackSpec UpAttack => GetBasicAttack(1); // 두 번째 기본 공격: 위로 치기(높은 부위용). 없으면 null

    public override ETeam Team => ETeam.Player;

    // ------------------------------------------------

    // 대시를 켜고 공격 끊기 시간을 넣는다
    public override void SetFeatures(UnitController unit)
    {
        unit.UnitExecutor.EnableDash(DashSpeed, DashTime, DashCooldown, IsDashInvincible, DashAnimSpeed);
        unit.UnitExecutor.Attack.CancelDelay = CancelDelay;
    }

    // 판단은 입력
    public override BrainBase CreateBrain(UnitController unit)
    {
        return new PlayerBrain(unit, this);
    }

#if UNITY_EDITOR
    public void EditorSetPrefab(UnitController prefab) => Prefab = prefab;
#endif
}
