using System;
using UnityEngine;

// ==================================================

// 전투 진행(리스폰·부활·쓰러짐)

[Serializable]
public class BattleDefine : DefineSpecBase
{
    [field: SerializeField, Tooltip("플레이어가 이보다 가까우면 리스폰을 미룬다(눈앞에 갑자기 생기지 않게)")]
    public float RespawnBlockDistance { get; private set; } = 15f;

    [field: SerializeField, Tooltip("쓰러졌을 때 부활에 드는 골드. 모자라면 부활 못 함")]
    public int ReviveGold { get; private set; } = 50; // [mj] todo) 가안

    [field: SerializeField, Tooltip("부활 직후 무적 시간(초). 체력은 가득 찬다")]
    public float ReviveInvincibleTime { get; private set; } = 2f;

    [field: SerializeField, Tooltip("쓰러진 뒤 부활/귀가 팝업까지(초). 그동안 주운 버섯을 땅에 떨구는 걸 보여 준다")]
    public float DownPopupDelay { get; private set; } = 2f;

    public override string DisplayName => "전투 진행";
}
