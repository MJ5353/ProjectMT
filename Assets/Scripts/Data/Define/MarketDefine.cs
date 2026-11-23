using System;
using UnityEngine;

// ==================================================

// 시장(거래·시세)과 상인 신뢰도

[Serializable]
public class MarketDefine : DefineSpecBase
{
    // 시장 ---
    [field: SerializeField, Tooltip("거래·기다리기 한 번에 줄어드는 밤(초). 화면 표기는 ActionMinutes")]
    public float ActionTime { get; private set; } = 15f;

    [field: SerializeField, Tooltip("거래·기다리기 한 번의 화면 표기(분)")]
    public int ActionMinutes { get; private set; } = 30;

    [field: SerializeField, Tooltip("밤마다 오는 상인 수")]
    public int MerchantCount { get; private set; } = 3;

    [field: SerializeField, Tooltip("거래 한 번에 고를 수 있는 묶음 수(같은 버섯·등급·신선도 = 한 묶음). 묶음마다 개수 제한은 없다")]
    public int TradeLimit { get; private set; } = 5;

    [field: SerializeField, Tooltip("덜 신선한 버섯 가격 배율")]
    public float LowFreshRate { get; private set; } = 0.6f; // [mj] todo) 가안

    [field: SerializeField, Tooltip("희귀 버섯 가격 배율(일반 1)")]
    public float RareRate { get; private set; } = 2.5f;

    [field: SerializeField, Tooltip("영웅 버섯 가격 배율(일반 1)")]
    public float EpicRate { get; private set; } = 6f;

    // 신뢰도 ---
    [field: SerializeField, Tooltip("상인 신뢰도 상한")]
    public int TrustMax { get; private set; } = 100;

    [field: SerializeField, Tooltip("거래 한 번에 오르는 신뢰도")]
    public int TrustPerTrade { get; private set; } = 5;

    [field: SerializeField, Tooltip("신뢰도 1당 가격 가산(100이면 +50%)")]
    public float TrustPriceRate { get; private set; } = 0.005f;

    [field: SerializeField, Tooltip("신뢰도 1당 방문 가중치 가산(100이면 3배)")]
    public float TrustVisitRate { get; private set; } = 0.02f;

    public override string DisplayName => "시장";
}
