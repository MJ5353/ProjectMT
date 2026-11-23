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
        [field: SerializeField]
        public float Height { get; private set; } = 0.6f;   // 튀어나오는 높이(나오는 자리 기준)

        [field: SerializeField]
        public float Spread { get; private set; } = 2f;     // 옆으로 퍼지는 반경

        [field: SerializeField]
        public float UpSpeed { get; private set; } = 5f;    // 위로 튀는 속도

        public DropSpec() { }

        public DropSpec(float height, float spread, float upSpeed)
        {
            Height = height;
            Spread = spread;
            UpSpeed = upSpeed;
        }
    }

    // 픽업 ---
    [field: SerializeField]
    public float PickupRadius { get; private set; } = 2f;           // 플레이어가 이 거리 안에 오면 빨려 들기 시작

    [field: SerializeField]
    public float PickupMagnetSpeed { get; private set; } = 12f;     // 빨려 드는 속도

    [field: SerializeField]
    public float PickupActivateDelay { get; private set; } = 0.5f;  // 떨어진 뒤 주울 수 있기까지(초)

    [field: SerializeField]
    public float PickupCollectDistance { get; private set; } = 0.5f; // 이 거리까지 빨려 오면 주운 것으로 친다

    // 튀어나오기 ---
    [field: SerializeField]
    public DropSpec DownDrop { get; private set; } = new(0.8f, 2.5f, 6f);   // 쓰러질 때 주운 버섯을 떨굴 때

    [field: SerializeField]
    public DropSpec MonsterDrop { get; private set; } = new(0.6f, 2f, 5f);  // 몬스터가 죽을 때

    [field: SerializeField]
    public DropSpec PartDrop { get; private set; } = new(0f, 2f, 5f);       // 부위(버섯)가 깨질 때. 높이는 부위 자리에서 바로 나오므로 0

    public override string DisplayName => "버섯 드롭·픽업";
}
