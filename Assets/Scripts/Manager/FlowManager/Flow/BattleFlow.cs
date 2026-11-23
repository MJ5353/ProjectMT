// ==================================================

public enum EBattleFlowState
{
    Start,  // 맵·플레이어·몬스터를 만들고 판단을 멈춘 채 시작 팝업(클릭하여 시작). 누르면 Play로
    Play,   // 밤이 흐르고 싸운다. 토벌 종료 → Market, 쓰러짐 → Down, 밤이 다 감 → Result(TimedOut)
    Down,   // 쓰러짐. 버섯을 떨구고 잠깐 뒤 사망 팝업. 부활 → Play, 귀가 → Result(GaveUp)
    Market, // 현장 시장(게임 시간 정지). 닫히면 Result(Exited)
    Result, // 보상 반영 후 결과 팝업. 다시 하기·로비
}

// 한 판이 끝난 이유. 결과 상태에 넘기는 값이다
public enum EHuntResult
{
    Exited,   // 토벌 종료. 시장에서 판 골드와 못 판 버섯(썩은 버섯)
    TimedOut, // 밤이 끝나 쫓겨남. 주운 버섯은 썩은 버섯이 된다
    GaveUp,   // 쓰러진 뒤 귀가. 아무것도 못 받는다
}

// ==================================================

// 전투 씬 흐름(한 판). 전투 전용 Service(Unit·HitStop·Spawn·Loot·Battle)를 들고, 지역(MapDef)과 맵(MapRoot)은 씬 컨트롤러가 넘긴다.
// 다시 하기는 씬을 다시 읽지 않고 이 Flow를 End → Begin으로 다시 돌린다(FlowManager.Restart). Service·상태는 새로 만들지 않고 Clear·Enter에서 되돌린다
// 단계 전환과 팝업 띄우기(시작·토벌 종료 확인·시장 닫기 확인 포함)는 여기와 상태가 맡고, Service는 데이터와 처리만 한다. 화면(ScreenBattle)은 표시와 요청만 한다.
//   Unit: 유닛 목록·플레이어 / Spawn: 맵 슬롯에서 스폰·리스폰 / Loot: 픽업·주운 버섯·보상 정산 / Battle: 밤 시간·쓰러짐·부활·클리어 저장
// Spawn·Loot·Battle은 서로 부르지 않는다. 순서가 필요한 일(스폰 뒤 플레이어 구독, 정산 뒤 저장)은 상태가 차례로 부른다. UnitService는 바탕이라 누구나 쓴다
// 상태 클래스는 BattleFlow.States.cs에 있다
// 접근: FlowManager.Instance.Battle?.UnitS / HitStopS / SpawnS / LootS / BattleS (전투가 아니면 Battle이 null)
// !!) 등록 순서 = Init 순서, Clear는 역순. 다른 Service가 UnitService를 쓰므로 Unit을 먼저 넣는다

public sealed partial class BattleFlow : FlowBase<EBattleFlowState>
{
    // service
    public UnitService UnitS { get; }
    public HitStopService HitStopS { get; }
    public SpawnService SpawnS { get; }
    public LootService LootS { get; }
    public BattleService BattleS { get; }

    public EHuntResult HuntResult { get; private set; } // Result 상태에서만 의미가 있다

    // ------------------------------------------------

    public BattleFlow(MapDef mapDef, MapRoot map)
    {
        UnitS = Register(new UnitService());
        HitStopS = Register(new HitStopService());
        SpawnS = Register(new SpawnService(mapDef, map));
        LootS = Register(new LootService(mapDef));
        BattleS = Register(new BattleService(mapDef));

        BattleS.PlayerDied += OnPlayerDied;

        AddState(EBattleFlowState.Start, new StartState(this));
        AddState(EBattleFlowState.Play, new PlayState(this));
        AddState(EBattleFlowState.Down, new DownState(this));
        AddState(EBattleFlowState.Market, new MarketState(this));
        AddState(EBattleFlowState.Result, new ResultState(this));
    }

    // 요청(화면·팝업이 부른다) ---

    // 토벌 종료 버튼. "토벌을 종료할까요?"를 묻고(묻는 동안 게임 시간 정지) 예면 현장 시장으로 간다
    public void ExitHunt()
    {
        if (State != EBattleFlowState.Play)
            return;

        PopupConfirm confirm = FlowManager.Instance.PopupS.Open<PopupConfirm>(EPopupType.PopupConfirm);
        if (confirm == null)
            return; // 원본이 없으면 PopupDef가 에러를 남긴다

        TimeManager.Instance.SetPause(EPauseReason.Confirm, true);
        confirm.Set(EStringKey.Battle_ExitConfirm, OnConfirmExit, OnCancelExit);
    }

    // 시작 팝업을 누름. 멈춰 둔 판단을 켜고 밤을 흐르게 한다
    public void BeginPlay()
    {
        if (State == EBattleFlowState.Start)
            ChangeState(EBattleFlowState.Play);
    }

    // 시장 닫기 버튼. 하루가 끝난다는 걸 묻고(시장 중이라 시간은 이미 멈춰 있다) 예면 시장을 닫는다.
    // 닫히면 HuntData가 보내는 MarketClosed를 Market 상태가 받아 결과로 넘어간다
    public void ExitMarket()
    {
        if (State != EBattleFlowState.Market)
            return;

        PopupConfirm confirm = FlowManager.Instance.PopupS.Open<PopupConfirm>(EPopupType.PopupConfirm);
        if (confirm == null)
            return; // 원본이 없으면 PopupDef가 에러를 남긴다

        // 남은 버섯이 없으면 썩는다는 말은 빼고 묻는다
        EStringKey message = PlayerDataManager.Instance.Hunt.HasHarvest ? EStringKey.Market_ExitConfirm : EStringKey.Market_ExitConfirmEmpty;
        confirm.Set(message, OnConfirmExitMarket);
    }

    // 사망 팝업의 부활. 골드가 모자라면 false
    public bool TryRevive()
    {
        if (State != EBattleFlowState.Down || BattleS.TryRevive() == false)
            return false;

        ChangeState(EBattleFlowState.Play);
        return true;
    }

    // 사망 팝업의 귀가
    public void GiveUp()
    {
        if (State == EBattleFlowState.Down)
            EndHunt(EHuntResult.GaveUp);
    }

    // 결과 팝업의 다시 하기. 로딩 화면을 덮고 이 Flow를 정리한 뒤 새로 시작한다(씬은 그대로)
    public bool Retry() => State == EBattleFlowState.Result && GameManager.Instance.RestartFlow();

    public bool TryGoLobby() => GameManager.Instance.ChangeScene(ESceneType.Lobby);

    // ---

    private void EndHunt(EHuntResult result)
    {
        if (State == EBattleFlowState.Result)
            return;

        HuntResult = result;
        ChangeState(EBattleFlowState.Result);
    }

    // 확인 멈춤을 풀고 시장으로(Market 상태가 시장 이유로 다시 멈춘다)
    private void OnConfirmExit()
    {
        TimeManager.Instance.SetPause(EPauseReason.Confirm, false);

        if (State == EBattleFlowState.Play)
            ChangeState(EBattleFlowState.Market);
    }

    // 아니오·뒤로 가기, 씬 전환으로 확인 팝업이 정리될 때도 불린다
    private void OnCancelExit() => TimeManager.Instance.SetPause(EPauseReason.Confirm, false);

    private void OnConfirmExitMarket()
    {
        if (State == EBattleFlowState.Market)
            PlayerDataManager.Instance.Hunt.EndMarket();
    }

    private void OnPlayerDied()
    {
        if (State == EBattleFlowState.Play)
            ChangeState(EBattleFlowState.Down);
    }

    // virtual ---

    protected override void OnBegin() => ChangeState(EBattleFlowState.Start);

    // 화면(종료 버튼 등)이 매 프레임 상태를 읽지 않게 바뀔 때 알린다
    protected override void OnStateChanged(EBattleFlowState state) => SEventManager.Publish(new BattleStateChanged(state));
}
