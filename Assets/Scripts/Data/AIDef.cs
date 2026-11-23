using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// AI 서브(유닛 수명). MonsterDef.SetAI가 공격 쿨 → 대상 감지 → 보스 감시 순으로 등록한다(Tick 순서)
[Flags]
public enum EAISub
{
    None = 0,
    TargetSense = 1 << 0,   // 대상 감지·스폰 자리. 대기·배회·추적·도망·숨기·포효, 공격 선택·보스 감시가 쓴다
    AttackPicker = 1 << 1,  // 공격 선택·공용 쿨. 추적·보스 감시가 쓴다
    BossWatch = 1 << 2,     // 보스 감시(첫 발견·분노 포효, 부위 파괴 쓰러짐). 포효·쓰러짐이 쓴다. 화면 위 보스 체력바도 이 서브로 보스를 가린다
}

// ==================================================

// AI 프리셋. 여러 유닛이 같은 에셋을 같이 쓴다(UnitDef.AI, 예: AI_Normal·AI_Elite·AI_Boss). 비어 있으면 AI가 없는 유닛(플레이어).
// 어떤 코어를 붙일지도 여기서 정한다(MonsterDef.SetAI). 서브는 코어가 쓰는 만큼 따라 붙는다(AICore.GetSubs). 공통 공격·피격·사망은 적지 않는다. 쓰지 않는 코어의 칸은 무시한다.
// 기본값은 일반 몬스터(AI_Normal) 묶음: 프리셋이 비었을 때 MonsterDef.SetAI가 이 기본값으로 만든다

[CreateAssetMenu(menuName = "MushTaker/Data/AI", fileName = "AI_")]
public class AIDef : DataDef
{
    // 조립 --- 데이터 검사(DataTools)가 AI 코어만·중복 없이, 기본·피격 뒤 코어가 목록에 있는지 본다
    [SerializeField]
    private List<ECoreState> m_ListCore = new() { ECoreState.Idle, ECoreState.Wander, ECoreState.Chase, ECoreState.Flee }; // 붙일 AI 코어(Idle·Wander·Chase·Flee·Burrow·Roar·Down·Follow)

    [field: SerializeField]
    public ECoreState DefaultCore { get; private set; } = ECoreState.Idle;     // 스폰·멈출 때 들어가는 코어. 보통 Idle, 펫은 Follow

    [field: SerializeField]
    public ECoreState AfterHitCore { get; private set; } = ECoreState.Chase;   // 피격이 끝나면 갈 코어(Chase·Flee·Burrow). 목록에 없는 값(Move)이면 피격 전 코어로 돌아간다

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
    public float FleeTime { get; private set; } = 0f;           // 도망: 맞은 뒤 도망치는 시간. 0이면 바로 반격

    [field: SerializeField]
    public float HideTime { get; private set; } = 1.5f;         // 숨기: 땅속에서 대상 쪽으로 파고드는 시간(초)

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

    // ------------------------------------------------
    
    public IReadOnlyList<ECoreState> ListCore => m_ListCore;

    public EAISub Subs => AICore.GetSubs(m_ListCore);   // 붙일 서브. 코어가 정한다(각 코어의 Needs)

    public bool HasSub(EAISub sub) => (Subs & sub) == sub;

}
