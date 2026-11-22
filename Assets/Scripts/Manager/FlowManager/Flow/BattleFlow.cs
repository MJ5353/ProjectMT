// ==================================================

public enum EBattleFlowState
{
    Start,  // 맵·플레이어·몬스터를 만들고 다음 프레임에 Play로
    Play,   // 밤이 흐르고 싸운다. 토벌 종료 → Market, 쓰러짐 → Down, 밤이 다 감 → Result(TimedOut)
    Down,   // 쓰러짐. 버섯을 떨구고 잠깐 뒤 사망 팝업. 부활 → Play, 귀가 → Result(GaveUp)
    Market, // 현장 시장(게임 시간 정지). 닫히면 Result(Exited)
    Result, // 보상 반영 후 결과 팝업. 다시 하기·로비
}

// 한 판이 끝난 이유. 결과 상태에 넘기는 값이다
public enum EHuntResult
{
    Exited,   // 토벌 종료. 시장에서 판 골드와 못 판 버섯(썩은 버섯), 시장이 없으면 주운 버섯 그대로
    TimedOut, // 밤이 끝나 쫓겨남. 주운 버섯은 썩은 버섯이 된다
    GaveUp,   // 쓰러진 뒤 귀가. 아무것도 못 받는다
}

// ==================================================

// 전투 씬 흐름(하룻밤 한 판). 전투 전용 Service(Unit·HitStop·Battle)를 들고, 지역(MapDef)과 맵(MapRoot)은 씬 컨트롤러가 넘긴다.
// 다시 하기는 씬을 다시 읽지 않고 이 Flow를 끝낸 뒤 같은 지역·맵으로 새 BattleFlow를 시작한다(FlowManager.Restart)
// 단계 전환과 팝업 띄우기는 여기 상태가 맡고, BattleService는 스폰·밤 시간·주운 버섯·보상 같은 데이터와 처리만 한다. 화면(ScreenBattle)은 표시와 요청만 한다.
// 접근: FlowManager.Instance.Battle?.UnitS / HitStopS / BattleS (전투가 아니면 Battle이 null)
// !!) 등록 순서 = Init 순서. BattleService가 스폰 때 UnitService를 쓰므로 Unit을 먼저 넣는다

public sealed class BattleFlow : FlowBase<EBattleFlowState>
{
    // service
    public UnitService UnitS { get; }
    public HitStopService HitStopS { get; }
    public BattleService BattleS { get; }

    public EHuntResult HuntResult { get; private set; } // Result 상태에서만 의미가 있다
    public bool IsHuntOver => State == EBattleFlowState.Market || State == EBattleFlowState.Result; // 몬스터 판단을 멈춘다(BrainBase)

    // ------------------------------------------------

    public BattleFlow(MapDef mapDef, MapRoot map)
    {
        UnitS = Register(new UnitService());
        HitStopS = Register(new HitStopService());
        BattleS = Register(new BattleService(mapDef, map));

        BattleS.PlayerDied += OnPlayerDied;

        AddState(EBattleFlowState.Start, new StartState(this));
        AddState(EBattleFlowState.Play, new PlayState(this));
        AddState(EBattleFlowState.Down, new DownState(this));
        AddState(EBattleFlowState.Market, new MarketState(this));
        AddState(EBattleFlowState.Result, new ResultState(this));
    }

    // 요청(화면·팝업이 부른다) ---

    // 토벌 종료(확인 팝업 뒤). 시장 팝업이 있으면 시장으로, 없으면 주운 그대로 받고 끝낸다
    public void ExitHunt()
    {
        if (State != EBattleFlowState.Play)
            return;

        PopupService popupS = FlowManager.Instance.PopupS;
        if (popupS != null && popupS.HasPrefab(EPopupType.PopupMarket))
            ChangeState(EBattleFlowState.Market);
        else
            EndHunt(EHuntResult.Exited);
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

    private void OnPlayerDied()
    {
        if (State == EBattleFlowState.Play)
            ChangeState(EBattleFlowState.Down);
    }

    // virtual ---

    protected override void OnBegin() => ChangeState(EBattleFlowState.Start);

    // state ---

    private sealed class StartState : FlowStateBase<BattleFlow>
    {
        public StartState(BattleFlow flow) : base(flow) { }

        public override void Enter() => Flow.BattleS.StartHunt();

        // 스폰한 프레임이 지나고 넘어간다(Enter 안에서 상태를 바꾸지 않는다)
        public override void Update(float deltaTime) => Flow.ChangeState(EBattleFlowState.Play);
    }

    // 밤 시계는 이 상태에서만 흐른다
    private sealed class PlayState : FlowStateBase<BattleFlow>
    {
        public PlayState(BattleFlow flow) : base(flow) { }

        public override void Update(float deltaTime)
        {
            BattleService battleS = Flow.BattleS;
            battleS.AdvanceNight(deltaTime);

            if (battleS.IsNightOver)
                Flow.EndHunt(EHuntResult.TimedOut);
        }
    }

    // 주운 버섯을 떨구는 걸 잠깐 보여 준 뒤 부활/귀가 팝업. 팝업은 고른 뒤 스스로 닫힌다
    private sealed class DownState : FlowStateBase<BattleFlow>
    {
        private bool m_IsPopupOpened;

        public DownState(BattleFlow flow) : base(flow) { }

        public override void Enter()
        {
            m_IsPopupOpened = false;
            Flow.BattleS.DropLoot();
        }

        public override void Update(float deltaTime)
        {
            if (m_IsPopupOpened || Flow.TimeInState < GameManager.Instance.Define.DownPopupDelay)
                return;

            m_IsPopupOpened = true;
            FlowManager.Instance.PopupS?.Open<PopupDeath>(EPopupType.PopupDeath);
        }
    }

    // 시장이 열린 동안 게임 시간을 멈춘다. 시장 팝업은 MarketClosed를 받으면 스스로 닫힌다
    private sealed class MarketState : FlowStateBase<BattleFlow>
    {
        public MarketState(BattleFlow flow) : base(flow) { }

        public override void Enter()
        {
            BattleService battleS = Flow.BattleS;

            TimeManager.Instance.SetPause(EPauseReason.Market, true);
            SEventManager.Subscribe<MarketClosed>(OnMarketClosed);

            PlayerDataManager.Instance.Hunt.BeginMarket(battleS.DicLoot, battleS.RemainTime, battleS.NightTime);
            FlowManager.Instance.PopupS?.Open<PopupMarket>(EPopupType.PopupMarket);
        }

        // 시장 도중 씬이 바뀌어도(Flow End) 멈춘 시간이 남지 않게 여기서 푼다
        public override void Exit()
        {
            SEventManager.Unsubscribe<MarketClosed>(OnMarketClosed);
            TimeManager.Instance.SetPause(EPauseReason.Market, false);
        }

        // 시장 닫기 또는 밤이 다 감. 골드·썩은 버섯·신뢰도는 HuntData가 이미 넣었다
        private void OnMarketClosed(MarketClosed evt)
        {
            Flow.BattleS.SetMarketReward(evt);
            Flow.EndHunt(EHuntResult.Exited);
        }
    }

    // 결과 팝업은 공용 PopupService가 들고 있어 Flow가 끝나도 닫히지 않는다. 다시 하기에 남지 않게 나갈 때 직접 닫는다
    private sealed class ResultState : FlowStateBase<BattleFlow>
    {
        private PopupResult m_Popup;

        public ResultState(BattleFlow flow) : base(flow) { }

        public override void Enter()
        {
            Flow.BattleS.Finish(Flow.HuntResult);
            m_Popup = FlowManager.Instance.PopupS?.Open<PopupResult>(EPopupType.PopupResult);
        }

        public override void Exit()
        {
            if (m_Popup != null)
                m_Popup.Close();

            m_Popup = null;
        }
    }
}
