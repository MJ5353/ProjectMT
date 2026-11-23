using System;
using UnityEngine;

// ==================================================

// 버섯 드롭·픽업. 튀어나오는 값은 쓰러짐·몬스터·부위 세 벌을 같은 모양(DropSpec)으로 둔다

[Serializable]
public class DropDefine : DefineSpecBase
{
    // 버섯이 튀어나오는 모양 한 벌
    [Serializable]
    public class DropSpec
    {
        [field: SerializeField, Tooltip("튀어나오는 높이(나오는 자리 기준)")]
        public float Height { get; private set; } = 0.6f;

        [field: SerializeField, Tooltip("옆으로 퍼지는 반경")]
        public float Spread { get; private set; } = 2f;

        [field: SerializeField, Tooltip("위로 튀는 속도")]
        public float UpSpeed { get; private set; } = 5f;

        public DropSpec() { }

        public DropSpec(float height, float spread, float upSpeed)
        {
            Height = height;
            Spread = spread;
            UpSpeed = upSpeed;
        }
    }

    // 픽업 ---
    [field: SerializeField, Tooltip("플레이어가 이 거리 안에 오면 빨려 들기 시작")]
    public float PickupRadius { get; private set; } = 2f;

    [field: SerializeField, Tooltip("빨려 드는 속도")]
    public float PickupMagnetSpeed { get; private set; } = 12f;

    [field: SerializeField, Tooltip("떨어진 뒤 주울 수 있기까지(초)")]
    public float PickupActivateDelay { get; private set; } = 0.5f;

    [field: SerializeField, Tooltip("이 거리까지 빨려 오면 주운 것으로 친다")]
    public float PickupCollectDistance { get; private set; } = 0.5f;

    // 튀어나오기 ---
    [field: SerializeField, Tooltip("쓰러질 때 주운 버섯을 떨굴 때")]
    public DropSpec DownDrop { get; private set; } = new(0.8f, 2.5f, 6f);

    [field: SerializeField, Tooltip("몬스터가 죽을 때")]
    public DropSpec MonsterDrop { get; private set; } = new(0.6f, 2f, 5f);

    [field: SerializeField, Tooltip("부위(버섯)가 깨질 때. 높이는 부위 자리에서 바로 나오므로 0")]
    public DropSpec PartDrop { get; private set; } = new(0f, 2f, 5f);

    public override string DisplayName => "버섯 드롭·픽업";
}
