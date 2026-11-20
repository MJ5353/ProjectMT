using UnityEngine;

// ==================================================

// 게임 전역 조절값 묶음. 특정 콘텐츠(플레이어·몬스터·지역)에 속하지 않는 서비스·연출 수치를 한곳에서 고친다.
// Resources/Data/Define.asset 하나만 두고 GameManager.Instance.Define으로 읽는다. 동작 보정값(중력·바닥 띄우기 등)은 코드 상수로 둔다

[CreateAssetMenu(menuName = "MushTaker/Data/Define", fileName = "Define")]
public class DefineDef : DataDef
{
    public const string DefineID = "Define";

    // 전투 진행 ---
    [field: SerializeField]
    public float RespawnBlockDistance { get; private set; } = 15f;  // 플레이어가 이보다 가까우면 리스폰을 미룬다(눈앞에 갑자기 생기지 않게)

    [field: SerializeField]
    public int ReviveGold { get; private set; } = 100;              // [mj] todo) 가안. 쓰러졌을 때 부활에 드는 골드. 모자라면 부활 못 함

    [field: SerializeField]
    public float ReviveInvincibleTime { get; private set; } = 2f;   // 부활 직후 무적 시간(초). 체력은 가득 찬다

    [field: SerializeField]
    public float DownPopupDelay { get; private set; } = 2f;         // 쓰러진 뒤 부활/귀가 팝업까지(초). 그동안 주운 버섯을 땅에 떨구는 걸 보여 준다

    [field: SerializeField]
    public float DownDropHeight { get; private set; } = 0.8f;       // 쓰러질 때 버섯이 튀어나오는 높이

    [field: SerializeField]
    public float DownDropSpread { get; private set; } = 2.5f;       // 쓰러질 때 버섯이 옆으로 퍼지는 반경

    [field: SerializeField]
    public float DownDropUpSpeed { get; private set; } = 6f;        // 쓰러질 때 버섯이 위로 튀는 속도

    // 버섯 픽업 ---
    [field: SerializeField]
    public float PickupRadius { get; private set; } = 1.8f;         // 플레이어가 이 거리 안에 오면 빨려 들기 시작

    [field: SerializeField]
    public float PickupMagnetSpeed { get; private set; } = 12f;     // 빨려 드는 속도

    [field: SerializeField]
    public float PickupActivateDelay { get; private set; } = 0.5f;  // 떨어진 뒤 주울 수 있기까지(초)

    [field: SerializeField]
    public float PickupCollectDistance { get; private set; } = 0.5f; // 이 거리까지 빨려 오면 주운 것으로 친다

    [field: SerializeField]
    public float MonsterDropHeight { get; private set; } = 0.6f;    // 몬스터가 죽을 때 버섯이 튀어나오는 높이

    [field: SerializeField]
    public float MonsterDropSpread { get; private set; } = 2f;      // 몬스터 드롭이 옆으로 퍼지는 반경

    [field: SerializeField]
    public float MonsterDropUpSpeed { get; private set; } = 5f;     // 몬스터 드롭이 위로 튀는 속도

    // 부위(버섯) ---
    [field: SerializeField]
    public float PartRegrowTime { get; private set; } = 1.5f;       // [mj] todo) 임시값. 다시 돋은 뒤 원래 크기까지 자라는 시간(초)

    [field: SerializeField]
    public float PartRegrowStartScale { get; private set; } = 0.3f; // 다시 돋을 때 처음 크기 배율

    [field: SerializeField]
    public float PartDropUpSpeed { get; private set; } = 5f;        // 깨진 버섯 픽업이 튀어 오르는 속도

    [field: SerializeField]
    public float PartDropSpread { get; private set; } = 2f;         // 깨진 버섯 픽업이 옆으로 퍼지는 속도

    // 체력바 ---
    [field: SerializeField]
    public float MonsterHpBarShowTime { get; private set; } = 3f;   // 몬스터가 맞은 뒤 머리 위 체력바가 보이는 시간(초)

    [field: SerializeField]
    public float MonsterHpBarOffset { get; private set; } = 0.4f;   // 몸 맨 위에서 체력바까지 높이

    // 보스 포효 연출 ---
    [field: SerializeField]
    public float RoarShake { get; private set; } = 0.06f;          // 포효할 때 카메라가 다가오는 세기(거리 비율)

    [field: SerializeField]
    public float RoarShakeTime { get; private set; } = 0.8f;       // 포효 흔들림 시간(초)

    [field: SerializeField]
    public float RoarEffectTime { get; private set; } = 1.6f;      // 붉은 가장자리·이름 배너가 떠 있는 시간(초)

    [field: SerializeField]
    public float RoarVignetteAlpha { get; private set; } = 0.7f;   // 붉은 가장자리 가장 진할 때 불투명도

    // 카메라 ---
    [field: SerializeField]
    public float CameraDistance { get; private set; } = 11f;        // 바라보는 점에서 카메라까지 거리

    [field: SerializeField]
    public float CameraPitch { get; private set; } = 50f;           // 내려다보는 각도

    [field: SerializeField]
    public float CameraYaw { get; private set; } = 0f;

    [field: SerializeField]
    public float CameraLookHeight { get; private set; } = 1f;       // 바라보는 점 높이(발밑 기준)

    [field: SerializeField]
    public float CameraSmoothTime { get; private set; } = 0.12f;    // 따라가는 부드러움(초). 작을수록 바짝 붙는다

    [field: SerializeField]
    public float ShakeInRatio { get; private set; } = 0.2f;         // 흔들림 시간 중 다가가는 구간 비율. 나머지는 부드럽게 복귀

    // 시장 ---
    [field: SerializeField]
    public float MarketActionTime { get; private set; } = 15f;      // 거래·기다리기 한 번에 줄어드는 밤(초). 화면 표기는 MarketActionMinutes

    [field: SerializeField]
    public int MarketActionMinutes { get; private set; } = 30;      // 거래·기다리기 한 번의 화면 표기(분)

    [field: SerializeField]
    public int MarketMerchantCount { get; private set; } = 3;       // 밤마다 오는 상인 수

    [field: SerializeField]
    public int MarketTradeLimit { get; private set; } = 5;          // 거래 한 번에 팔 수 있는 버섯 수

    [field: SerializeField]
    public float MarketLowFreshRate { get; private set; } = 0.6f;   // [mj] todo) 가안. 덜 신선한 버섯 가격 배율

    [field: SerializeField]
    public int TrustMax { get; private set; } = 100;                // 상인 신뢰도 상한

    [field: SerializeField]
    public int TrustPerTrade { get; private set; } = 5;             // 거래 한 번에 오르는 신뢰도

    [field: SerializeField]
    public float TrustPriceRate { get; private set; } = 0.005f;     // 신뢰도 1당 가격 가산(100이면 +50%)

    [field: SerializeField]
    public float TrustVisitRate { get; private set; } = 0.02f;      // 신뢰도 1당 방문 가중치 가산(100이면 3배)
}
