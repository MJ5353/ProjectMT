using System.Collections.Generic;
using UnityEngine;

// ==================================================

// AI 코어(판단 + 이동)의 바탕. 코어마다 파일 하나(Core/AI/). MonsterDef.SetAI가 AI 프리셋(AIDef.ListCore)대로 등록한다.
// Step(Update)은 걷는 쪽으로 돌기만 하고(MoveCore), 판단은 Think에서 한다: 서브(공격 쿨 → 대상 감지 → 보스 감시) 다음이라 예전 AIBrain.Think 순서 그대로다.
// TimeInState는 들어온 뒤 판단이 돈 시간(예전 StateMachine.TimeInState). 다른 AI 코어로는 StateFeature.ChangeCore로 간다.
// 흐름: 대기 ↔ 배회 → (발견) 추적 → (공격 요청) → 추적 / 맞으면(피격 뒤 SetAfterHitCore) 도망·숨기 → 추적 / 보스는 포효·쓰러짐을 끼운다(BossWatchSub)

public abstract class AICore : MoveCore
{
    protected readonly AIDef Def;
    protected readonly TargetSenseSub Sense;     // 대상·스폰 자리. 서브는 코어보다 먼저 붙어(MonsterDef.SetAI) 생성자에서 찾는다. 없으면 null(펫)
    protected readonly AttackPickerSub Picker;   // 공격 선택·공용 쿨
    protected readonly BossWatchSub Watch;       // 보스 감시

    protected float TimeInState { get; private set; }
    protected TargetSelector Target => Sense.Selector;
    protected float MoveSpeed => Owner.Def.MoveSpeed;   // 추적·땅속 이동·따라가기

    // ------------------------------------------------

    protected AICore(UnitController owner, AIDef def) : base(owner)
    {
        Def = def;
        
        owner.StateF.TryGetSub(out Sense);
        owner.StateF.TryGetSub(out Picker);
        owner.StateF.TryGetSub(out Watch);
    }

    // 코어 종류가 쓰는 서브(각 코어의 Needs). AI 코어가 아니면 false
    public static bool TryGetNeeds(ECoreState type, out EAISub needs)
    {
        switch (type)
        {
            case ECoreState.Idle: needs = IdleCore.Needs; return true;
            case ECoreState.Wander: needs = WanderCore.Needs; return true;
            case ECoreState.Chase: needs = ChaseCore.Needs; return true;
            case ECoreState.Flee: needs = FleeCore.Needs; return true;
            case ECoreState.Follow: needs = FollowCore.Needs; return true;
            case ECoreState.Burrow: needs = BurrowCore.Needs; return true;
            case ECoreState.Roar: needs = RoarCore.Needs; return true;
            case ECoreState.Down: needs = DownCore.Needs; return true;
        }

        needs = EAISub.None;
        return false;
    }

    // 코어 목록이 쓰는 서브를 모으고, 서브끼리 쓰는 서브를 더한다(공격 선택 → 대상 감지, 보스 감시 → 대상 감지·공격 선택)
    public static EAISub GetSubs(IReadOnlyList<ECoreState> listCore)
    {
        // IReadOnlyList를 foreach로 돌면 열거자가 박싱돼 할당된다. 인덱스로 돈다
        EAISub subs = EAISub.None;
        for (int i = 0; i < listCore.Count; ++i)
        {
            if (TryGetNeeds(listCore[i], out EAISub needs))
                subs |= needs;
        }

        if ((subs & EAISub.BossWatch) != 0)
            subs |= EAISub.TargetSense | EAISub.AttackPicker;

        if ((subs & EAISub.AttackPicker) != 0)
            subs |= EAISub.TargetSense;

        return subs;
    }

    public sealed override void Think(float deltaTime)
    {
        TimeInState += deltaTime;
        Decide(deltaTime);
    }

    // 방향은 수평으로 펴서 정규화한다. speed가 0이면 멈춰서 Idle
    protected void Move(Vector3 direction, float speed, bool isRun = false)
    {
        direction.y = 0f;
        SetMove(direction.normalized * speed, isRun);
    }

    protected void ChangeCore(ECoreState type)
    {
        Owner.StateF.ChangeCore(type);
    }

    // virtual ---

    protected override void Enter()
    {
        base.Enter();
        TimeInState = 0f;
    }

    protected abstract void Decide(float deltaTime);
}
