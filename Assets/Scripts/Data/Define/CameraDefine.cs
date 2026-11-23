using System;
using UnityEngine;

// ==================================================

// 카메라 따라가기·흔들림

[Serializable]
public class CameraDefine : DefineSpecBase
{
    [field: SerializeField]
    public float Distance { get; private set; } = 11f;        // 바라보는 점에서 카메라까지 거리

    [field: SerializeField]
    public float Pitch { get; private set; } = 50f;           // 내려다보는 각도

    [field: SerializeField]
    public float Yaw { get; private set; } = 0f;              // 옆으로 도는 각도

    [field: SerializeField]
    public float LookHeight { get; private set; } = 1f;       // 바라보는 점 높이(발밑 기준)

    [field: SerializeField]
    public float SmoothTime { get; private set; } = 0.12f;    // 따라가는 부드러움(초). 작을수록 바짝 붙는다

    [field: SerializeField]
    public float ShakeInRatio { get; private set; } = 0.2f;         // 흔들림 시간 중 다가가는 구간 비율. 나머지는 부드럽게 복귀

    // 보스 포효 ---
    [field: SerializeField]
    public float RoarShake { get; private set; } = 0.06f;           // 포효할 때 카메라가 다가오는 세기(거리 비율)

    [field: SerializeField]
    public float RoarShakeTime { get; private set; } = 0.8f;        // 포효 흔들림 시간(초)

    public override string DisplayName => "카메라";
}
