using System.Collections.Generic;

// ==================================================

// 보스 감시(AI 서브, 유닛 수명, Boss만). 대상을 처음 잡으면 BossAggroed(화면 위 보스 체력바), 맞아서 체력이 Def.EnrageHpRate 이하가 되면 분노(공격 후보·공용 쿨),
// 첫 발견·분노 뒤에는 포효를, 부위가 깨지면 쓰러짐을 끼운다. 끼우기는 행동(공격·피격·대시, 예약된 콤보 포함)이 없을 때만 쓰러짐 > 포효 순(포효는 추적 중일 때)

public class BossWatchSub : SubState
{
    private readonly AIDef m_Def;
    private readonly IReadOnlyList<SkillDef> m_ListEnrageSkill;   // MonsterDef.ListEnrageSkill
    private readonly AttackPickerSub m_Picker;

    private bool m_IsRoarPending;   // 다음에 쫓을 때 포효
    private bool m_IsDownPending;   // 행동(피격 경직)이 끝나면 쓰러짐

    public override ESubLifetime Lifetime => ESubLifetime.Unit;
    public bool IsEnraged { get; private set; }

    // ------------------------------------------------

    // 부위(PartF)는 AI보다, 대상 감지·공격 선택 서브는 이 서브보다 먼저 붙는다(MonsterDef.SetAI). 보스 감시를 쓰면 둘도 함께 붙는다(AICore.GetSubs)
    public BossWatchSub(UnitController owner, AIDef def, IReadOnlyList<SkillDef> listEnrageSkill) : base(owner)
    {
        m_Def = def;
        m_ListEnrageSkill = listEnrageSkill;

        owner.PartF.Broken += OnPartBroken;
        if (owner.StateF.TryGetSub(out TargetSenseSub sense) == false || owner.StateF.TryGetSub(out m_Picker) == false)
        {
            CLogger.Error(nameof(BossWatchSub), $"AI 프리셋 {def.name}에 보스 감시가 쓰는 대상 감지·공격 선택 서브가 없습니다", owner);
            return;
        }

        sense.Selector.Acquired += OnTargetAcquired;
    }

    // 꺼낼 때·다시 켤 때(다시 켜는 건 스폰 직후 시작 팝업뿐이라 스폰 때와 같다)
    protected override void Enter()
    {
        m_IsRoarPending = true;
        m_IsDownPending = false;
        IsEnraged = false;
    }

    protected override void Tick(float deltaTime)
    {
        if (Owner.IsDead || IsActing())
            return;

        if (m_IsDownPending)
        {
            m_IsDownPending = false;
            Owner.StateF.ChangeCore(ECoreState.Down);
            return;
        }

        if (m_IsRoarPending && Owner.StateF.Current == ECoreState.Chase)
        {
            m_IsRoarPending = false;
            Owner.StateF.ChangeCore(ECoreState.Roar);
        }
    }

    // 체력이 깎인 뒤라 여기서 분노를 확인한다
    public override void OnHit(in HitParams hit)
    {
        if (IsEnraged || m_Def.EnrageHpRate <= 0f)
            return;

        HealthFeature health = Owner.HealthF;
        if (health.Current > health.MaxHp * m_Def.EnrageHpRate)
            return;

        IsEnraged = true;
        m_Picker.Enrage(m_ListEnrageSkill, m_Def.EnrageCooldownRate);
        m_IsRoarPending = true;
    }

    // ==================================================

    private void OnPartBroken(PartFeature.Part part)
    {
        if (Owner.IsDead == false)
            m_IsDownPending = true;
    }

    private void OnTargetAcquired()
    {
        SEventManager.Publish(new BossAggroed(Owner));
    }

    // 공격·피격·대시 중이거나 콤보 다음 타가 예약돼 있으면(공격이 끝난 프레임, AttackPickerSub) 끼우지 않는다
    private bool IsActing()
    {
        StateFeature state = Owner.StateF;
        ECoreState current = state.Current;
        return current == ECoreState.Attack || current == ECoreState.Hit || current == ECoreState.Dash || state.ReservedAttack != null;
    }
}
