using UnityEngine;

// ==================================================

// AI 종류. MonsterDef.SetAI가 이 값으로 코어·서브 묶음을 등록한다
public enum EAIType
{
    Normal,     // 대기·배회·추적·도망
    Burrow,     // 도망 대신 숨기: 맞으면 땅속에 숨었다가 대상 근처에서 나온다(버더지)
    Boss,       // 도망 대신 포효(첫 발견·분노)·쓰러짐(부위가 깨지면), 체력이 줄면 분노
    Pet,        // 주인 따라가기만
}

// ==================================================

// AI 프리셋. 여러 유닛이 같은 에셋을 같이 쓴다(UnitDef.AI, 예: AI_Normal·AI_Elite·AI_Boss). 비어 있으면 AI가 없는 유닛(플레이어).
// AI 코어·서브는 이것과 유닛 이동 속도(UnitDef.MoveSpeed)만 읽는다. 종류(Type)에 따라 쓰지 않는 칸은 무시한다

[CreateAssetMenu(menuName = "MushTaker/Data/AI", fileName = "AI_")]
public class AIDef : DataDef
{
    [field: SerializeField]
    public EAIType Type { get; private set; } = EAIType.Normal;

    // 이동 --- 기준 속도는 UnitDef.MoveSpeed(추적·땅속 이동·따라가기)
    [field: SerializeField]
    public float WanderSpeedRate { get; private set; } = 0.4f;  // 배회 속도 = MoveSpeed × 이 값

    [field: SerializeField]
    public float FleeSpeed { get; private set; } = 4f;          // 도망 속도

    // 감지·거리 ---
    [field: SerializeField]
    public float SightRange { get; private set; } = 7f;         // 적을 알아채는 거리

    [field: SerializeField]
    public float LoseRange { get; private set; } = 12f;         // 추적을 포기하는 거리

    [field: SerializeField]
    public float WanderRadius { get; private set; } = 4f;       // 스폰 자리에서 배회하는 반경

    [field: SerializeField]
    public float AttackRange { get; private set; } = 1.4f;      // 추적을 멈추고 마주 보는 거리. 공격 선택은 AttackSpec.MinRange·MaxRange

    [field: SerializeField]
    public float AttackCooldown { get; private set; } = 1.2f;   // 공격 하나가 끝난 뒤 다음 공격까지 공용 쿨(초)

    // 맞은 뒤 ---
    [field: SerializeField]
    public float FleeTime { get; private set; } = 0f;           // Normal: 맞은 뒤 도망치는 시간. 0이면 바로 반격

    [field: SerializeField]
    public float HideTime { get; private set; } = 1.5f;         // Burrow: 땅속에서 대상 쪽으로 파고드는 시간(초)

    // 보스 ---
    [field: SerializeField]
    public float DownTime { get; private set; } = 2.5f;         // 부위가 깨진 뒤 쓰러져 있는 시간(초). PartBreak 애니 뒤에 Down 애니

    [field: SerializeField]
    public float EnrageHpRate { get; private set; } = 0.5f;     // 체력이 이 비율 이하가 되면 분노. 0이면 분노 없음

    [field: SerializeField]
    public float EnrageCooldownRate { get; private set; } = 0.6f; // 분노 뒤 공용 쿨 배율. 분노 뒤 더할 공격은 몬스터마다(MonsterDef.ListEnrageSkill)

    // 동료 ---
    [field: SerializeField]
    public float FollowDistance { get; private set; } = 2f;     // 주인과 이보다 멀어지면 따라간다
}
