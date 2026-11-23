using System;
using UnityEngine;

// ==================================================

// 대시 수치 한 벌. PlayerDef.Dash가 들고 StateFeature.EnableDash로 DashCore에 넘긴다.
// 시간·쿨은 기본값이고, 최종값은 대시할 때마다 능력치(StatFeature)에서 읽는다

[Serializable]
public class DashSpec
{
    [field: SerializeField]
    public float Speed { get; private set; } = 16f;             // 대시 속도

    [field: SerializeField]
    public float Time { get; private set; } = 0.18f;            // 대시 지속 시간(초)

    [field: SerializeField]
    public float Cooldown { get; private set; } = 0.5f;         // 대시가 끝난 뒤 다시 쓰기까지(초)

    [field: SerializeField]
    public float MinCooldownRate { get; private set; } = 0.2f;  // 회피를 아무리 올려도 쿨은 이 비율 밑으로 내려가지 않는다

    [field: SerializeField]
    public bool IsInvincible { get; private set; } = true;      // 대시 중 무적

    [field: SerializeField]
    public float AnimSpeed { get; private set; } = 1.6f;        // 대시 중 Run 애니 배속

    [field: SerializeField]
    public float PerfectDodgeWindow { get; private set; } = 0.8f; // [mj] todo) 임시값. 판정 전 이 시간(초) 안에 공격 범위에 들어와 있다가, 대시를 쓴 뒤 이 시간 안에 판정을 안 맞으면 퍼펙트 닷지. 0이면 끔
}
