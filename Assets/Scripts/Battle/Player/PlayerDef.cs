using UnityEngine;

// 채집꾼 수치. 몸 수치(UnitDef)에 이동·공격 보조·대시를 더한다
[CreateAssetMenu(menuName = "MushTaker/Data/Player", fileName = "Player")]
public class PlayerDef : UnitDef
{
    // 이동---
    [SerializeField] private float m_MoveSpeed = 5f;
    [SerializeField] private float m_RunThreshold = 0.6f;    // 입력 세기가 이 값 이상이면 Run 애니

    // 공격 보조---
    [SerializeField] private float m_AutoAimRange = 3.5f;    // 이 거리 안의 몬스터 쪽으로 자동 회전
    [SerializeField] private float m_CancelDelay = 0.12f;    // 판정 뒤 이 시간이 지나면 다음 공격으로 끊을 수 있음

    // 대시---
    [SerializeField] private float m_DashSpeed = 16f;
    [SerializeField] private float m_DashTime = 0.18f;       // 대시 지속 시간(초)
    [SerializeField] private float m_DashCooldown = 0.5f;    // 대시가 끝난 뒤 다시 쓰기까지
    [SerializeField] private bool m_IsDashInvincible = true; // 대시 중 무적
    [SerializeField] private float m_DashAnimSpeed = 1.6f;   // 대시 중 Run 애니 배속

    public float MoveSpeed => m_MoveSpeed;
    public float RunThreshold => m_RunThreshold;
    public float AutoAimRange => m_AutoAimRange;
    public float CancelDelay => m_CancelDelay;
    public float DashSpeed => m_DashSpeed;
    public float DashTime => m_DashTime;
    public float DashCooldown => m_DashCooldown;
    public bool IsDashInvincible => m_IsDashInvincible;
    public float DashAnimSpeed => m_DashAnimSpeed;
}
