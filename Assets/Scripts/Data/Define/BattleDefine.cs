using System;
using UnityEngine;

// ==================================================

// 전투 진행(리스폰·부활·쓰러짐)

[Serializable]
public class BattleDefine : DefineSpecBase
{
    [field: SerializeField]
    public float RespawnBlockDistance { get; private set; } = 15f;  // 플레이어가 이보다 가까우면 리스폰을 미룬다(눈앞에 갑자기 생기지 않게)

    [field: SerializeField]
    public int ReviveGold { get; private set; } = 50;               // [mj] todo) 가안. 쓰러졌을 때 부활에 드는 골드. 모자라면 부활 못 함

    [field: SerializeField]
    public float ReviveInvincibleTime { get; private set; } = 2f;   // 부활 직후 무적 시간(초). 체력은 가득 찬다

    [field: SerializeField]
    public float DownPopupDelay { get; private set; } = 2f;         // 쓰러진 뒤 부활/귀가 팝업까지(초). 그동안 주운 버섯을 땅에 떨구는 걸 보여 준다

    public override string DisplayName => "전투 진행";
}
