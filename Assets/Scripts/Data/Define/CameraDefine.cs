using System;
using UnityEngine;

// ==================================================

// 카메라 따라가기·흔들림

[Serializable]
public class CameraDefine : DefineSpecBase
{
    [field: SerializeField, Tooltip("바라보는 점에서 카메라까지 거리")]
    public float Distance { get; private set; } = 11f;

    [field: SerializeField, Tooltip("내려다보는 각도")]
    public float Pitch { get; private set; } = 50f;

    [field: SerializeField, Tooltip("옆으로 도는 각도")]
    public float Yaw { get; private set; } = 0f;

    [field: SerializeField, Tooltip("바라보는 점 높이(발밑 기준)")]
    public float LookHeight { get; private set; } = 1f;

    [field: SerializeField, Tooltip("따라가는 부드러움(초). 작을수록 바짝 붙는다")]
    public float SmoothTime { get; private set; } = 0.12f;

    [field: SerializeField, Tooltip("흔들림 시간 중 다가가는 구간 비율. 나머지는 부드럽게 복귀")]
    public float ShakeInRatio { get; private set; } = 0.2f;

    // 보스 포효 ---
    [field: SerializeField, Tooltip("포효할 때 카메라가 다가오는 세기(거리 비율)")]
    public float RoarShake { get; private set; } = 0.06f;

    [field: SerializeField, Tooltip("포효 흔들림 시간(초)")]
    public float RoarShakeTime { get; private set; } = 0.8f;

    public override string DisplayName => "카메라";
}
