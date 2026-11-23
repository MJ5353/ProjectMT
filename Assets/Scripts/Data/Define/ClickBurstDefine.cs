using System;
using UnityEngine;

// ==================================================

// 클릭 버섯 연출(타이틀·로딩·로비)

[Serializable]
public class ClickBurstDefine : DefineSpecBase
{
    [field: SerializeField, Tooltip("한 번 누를 때 튀는 버섯 수(최소~최대)")]
    public Vector2Int Count { get; private set; } = new(2, 4);

    [field: SerializeField, Tooltip("튀어 퍼졌다 사라지기까지(초). 멈춘 시간에도 흐른다")]
    public float Time { get; private set; } = 0.4f;

    [field: SerializeField, Tooltip("버섯 크기(px, 최소~최대)")]
    public Vector2 Size { get; private set; } = new(100f, 100f);

    [field: SerializeField, Tooltip("퍼지는 거리(px, 최소~최대)")]
    public Vector2 Distance { get; private set; } = new(50f, 100f);

    [field: SerializeField, Tooltip("처음 기울기(±도)")]
    public float Tilt { get; private set; } = 30f;

    [field: SerializeField, Tooltip("사라질 때까지 더 도는 각도(±도)")]
    public float Spin { get; private set; } = 90f;

    [field: SerializeField, Tooltip("끝날 때 크기 배율")]
    public float EndScale { get; private set; } = 0.3f;

    [field: SerializeField, Tooltip("이 진행도(0~1)부터 흐려진다")]
    public float FadeStart { get; private set; } = 0.5f;

    public override string DisplayName => "클릭 버섯 연출";
}
