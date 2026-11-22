// ==================================================

// [mj] todo) 묶음 3에서 Start·Down·Market·Result를 더하고 EBattleState를 없앤다
public enum EBattleFlowState
{
    Play,
}

// ==================================================

// 전투 씬 흐름(하룻밤 한 판). 전투 전용 Service(Unit·HitStop·Battle)를 들고, 지역(MapDef)은 씬 컨트롤러가 골라 넘긴다.
// 접근: FlowManager.Instance.Battle?.UnitS / HitStopS / BattleS (전투가 아니면 Battle이 null)
// !!) 등록 순서 = Init 순서. BattleService가 Init에서 UnitService로 유닛을 만들므로 Unit을 먼저 넣는다

public sealed class BattleFlow : FlowBase<EBattleFlowState>
{
    // service
    public UnitService UnitS { get; }
    public HitStopService HitStopS { get; }
    public BattleService BattleS { get; }

    // ------------------------------------------------

    public BattleFlow(MapDef mapDef)
    {
        UnitS = Register(new UnitService());
        HitStopS = Register(new HitStopService());
        BattleS = Register(new BattleService(mapDef));

        AddState(EBattleFlowState.Play, new PlayState(this));
    }

    // virtual ---

    protected override void OnBegin() => ChangeState(EBattleFlowState.Play);

    // state ---

    private sealed class PlayState : FlowStateBase<BattleFlow>
    {
        public PlayState(BattleFlow flow) : base(flow) { }
    }
}
