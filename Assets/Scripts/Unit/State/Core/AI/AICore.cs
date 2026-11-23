using UnityEngine;

// ==================================================

// AI 코어(판단 + 이동)의 바탕. 코어마다 파일 하나(Core/AI/). MonsterDef.SetAI가 AI 종류(AIDef.Type)에 따라 골라 등록한다.
// Step(Update)은 걷는 쪽으로 돌기만 하고(MoveCore), 판단은 Think에서 한다: 서브(공격 쿨 → 대상 감지 → 보스 감시) 다음이라 예전 AIBrain.Think 순서 그대로다.
// TimeInState는 들어온 뒤 판단이 돈 시간(예전 StateMachine.TimeInState). 다른 AI 코어로는 StateFeature.ChangeCore로 간다.
// 흐름: 대기 ↔ 배회 → (발견) 추적 → (공격 요청) → 추적 / 맞으면(피격 뒤 SetAfterHitCore) 도망·숨기 → 추적 / 보스는 포효·쓰러짐을 끼운다(BossWatchSub)

public abstract class AICore : MoveCore
{
    protected readonly AIDef Def;
    protected readonly TargetSenseSub Sense;     // 대상·스폰 자리. 펫(따라가기)은 null

    protected float TimeInState { get; private set; }
    protected TargetSelector Target => Sense.Selector;
    protected float MoveSpeed => Owner.Def.MoveSpeed;   // 추적·땅속 이동·따라가기

    // ------------------------------------------------

    protected AICore(UnitController owner, AIDef def, TargetSenseSub sense) : base(owner)
    {
        Def = def;
        Sense = sense;
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
