using System;
using UnityEngine;

// ==================================================

// 전투 HUD 연출(몬스터 체력바·보스 포효·퍼펙트 닷지 러시)

[Serializable]
public class HudDefine : DefineSpecBase
{
    // 몬스터 체력바 ---
    [field: SerializeField, Tooltip("몬스터가 맞은 뒤 머리 위 체력바가 보이는 시간(초)")]
    public float MonsterHpBarShowTime { get; private set; } = 3f;

    [field: SerializeField, Tooltip("몸 맨 위에서 체력바까지 높이")]
    public float MonsterHpBarOffset { get; private set; } = 0.4f;

    // 보스 포효 ---
    [field: SerializeField, Tooltip("붉은 가장자리·이름 배너가 떠 있는 시간(초)")]
    public float RoarEffectTime { get; private set; } = 1.6f;

    [field: SerializeField, Tooltip("붉은 가장자리 가장 진할 때 불투명도")]
    public float RoarVignetteAlpha { get; private set; } = 0.7f;

    // 퍼펙트 닷지 러시 ---
    [field: SerializeField, Tooltip("러시 중 화면 어둠 불투명도")]
    public float RushDimAlpha { get; private set; } = 0.75f;

    [field: SerializeField, Tooltip("플레이어 주변 밝은 원 반지름(캔버스 단위, 1920x1080 기준)")]
    public float RushLightRadius { get; private set; } = 240f;

    [field: SerializeField, Tooltip("밝은 원 가장자리가 어둠으로 번지는 폭")]
    public float RushLightSoftness { get; private set; } = 140f;

    [field: SerializeField, Tooltip("어둠이 깔리고 걷히는 시간(초)")]
    public float RushDimFadeTime { get; private set; } = 0.15f;

    [field: SerializeField, Tooltip("\"퍼펙트 닷지!\" 문구가 떠 있는 시간(초)")]
    public float RushBannerTime { get; private set; } = 1f;

    public override string DisplayName => "전투 HUD";
}
