using UnityEngine;

// ==================================================

// 게임 전역 조절값 묶음. 특정 콘텐츠(플레이어·몬스터·지역)에 속하지 않는 서비스·연출 수치를 한곳에서 고친다.
// Resources/Data/Define.asset 하나만 두고 GameManager.Instance.Define으로 읽는다. 동작 보정값(중력·바닥 띄우기 등)은 코드 상수로 둔다.
// 값은 성격별 스펙(DefineSpecBase)으로 나눠 들고(Define.Drop.PickupRadius), 편집은 MushTaker/Define 편집기에서 스펙 단위로 한다

[CreateAssetMenu(menuName = "MushTaker/Data/Define", fileName = "Define")]
public class DefineDef : DataDef
{
    public const string DefineID = "Define";

    [field: SerializeField]
    public LoadingDefine Loading { get; private set; } = new();         // 씬 전환

    [field: SerializeField]
    public BattleDefine Battle { get; private set; } = new();           // 전투 진행(리스폰·부활·쓰러짐)

    [field: SerializeField]
    public DropDefine Drop { get; private set; } = new();               // 버섯 드롭·픽업

    [field: SerializeField]
    public PartDefine Part { get; private set; } = new();               // 부위(버섯) 다시 돋기

    [field: SerializeField]
    public CameraDefine Camera { get; private set; } = new();           // 카메라 따라가기·흔들림

    [field: SerializeField]
    public HudDefine Hud { get; private set; } = new();                 // 전투 HUD 연출

    [field: SerializeField]
    public MarketDefine Market { get; private set; } = new();           // 시장·신뢰도

    [field: SerializeField]
    public ClickBurstDefine ClickBurst { get; private set; } = new();   // 클릭 버섯 연출
}
