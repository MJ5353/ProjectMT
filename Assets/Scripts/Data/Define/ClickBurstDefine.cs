using System;
using UnityEngine;

// ==================================================

// 클릭 버섯 연출(타이틀·로딩·로비)

[Serializable]
public class ClickBurstDefine : DefineSpecBase
{
    [field: SerializeField]
    public Vector2Int Count { get; private set; } = new(2, 4);          // 한 번 누를 때 튀는 버섯 수(최소~최대)

    [field: SerializeField]
    public float Time { get; private set; } = 0.4f;                     // 튀어 퍼졌다 사라지기까지(초). 멈춘 시간에도 흐른다

    [field: SerializeField]
    public Vector2 Size { get; private set; } = new(100f, 100f);         // 버섯 크기(px, 최소~최대)

    [field: SerializeField]
    public Vector2 Distance { get; private set; } = new(50f, 100f);     // 퍼지는 거리(px, 최소~최대)

    [field: SerializeField]
    public float Tilt { get; private set; } = 30f;                      // 처음 기울기(±도)

    [field: SerializeField]
    public float Spin { get; private set; } = 90f;                      // 사라질 때까지 더 도는 각도(±도)

    [field: SerializeField]
    public float EndScale { get; private set; } = 0.3f;                 // 끝날 때 크기 배율

    [field: SerializeField]
    public float FadeStart { get; private set; } = 0.5f;                // 이 진행도(0~1)부터 흐려진다

    public override string DisplayName => "클릭 버섯 연출";
}
