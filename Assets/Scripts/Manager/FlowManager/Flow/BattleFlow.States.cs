// ==================================================

// BattleFlow의 상태들. 상태마다 Enter·Update·Exit에서 할 일만 두고, 상태를 넘기는 요청은 BattleFlow 함수(EndHunt 등)를 쓴다

public sealed partial class BattleFlow
{
    // 스폰한 뒤 판단을 멈추고 시작 팝업(딤 + 클릭하여 시작)을 띄운다. 누르면 BeginPlay로 Play. 다시 하기에도 매번 띄운다.
    // 밤 시계는 Play에서만 흐르니 시간은 멈추지 않는다(대기 애니는 돈다)
    private sealed class StartState : FlowStateBase<BattleFlow>
    {
        private PopupHuntStart m_Popup;

        public StartState(BattleFlow flow) : base(flow) { }

        public override void Enter()
        {
            Flow.SpawnS.SpawnAll();
            Flow.BattleS.WatchPlayer();
            Flow.UnitS.StopBrains();
            m_Popup = FlowManager.Instance.PopupS?.Open<PopupHuntStart, BattleFlow>(EPopupType.PopupHuntStart, Flow);
        }

        // 팝업 원본이 없으면(아직 안 만듦) 예전처럼 다음 프레임에 바로 시작한다(Enter 안에서 상태를 바꾸지 않는다)
        public override void Update(float deltaTime)
        {
            if (m_Popup == null)
                Flow.ChangeState(EBattleFlowState.Play);
        }

        // 누르면 팝업이 스스로 닫힌다. 시작 전에 씬이 바뀌어도 남지 않게 여기서도 닫는다
        public override void Exit()
        {
            if (m_Popup != null)
                m_Popup.Close();

            m_Popup = null;
            Flow.UnitS.ResumeBrains();
        }
    }

    // 밤 시계와 리스폰은 이 상태에서만 흐른다
    private sealed class PlayState : FlowStateBase<BattleFlow>
    {
        public PlayState(BattleFlow flow) : base(flow) { }

        public override void Update(float deltaTime)
        {
            BattleService battleS = Flow.BattleS;
            battleS.AdvanceNight(deltaTime);
            Flow.SpawnS.UpdateRespawn(deltaTime);

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

            UnitController player = Flow.UnitS.Player;
            if (player != null)
                Flow.LootS.DropLoot(player.transform.position);
        }

        public override void Update(float deltaTime)
        {
            if (m_IsPopupOpened || Flow.TimeInState < GameManager.Instance.Define.Battle.DownPopupDelay)
                return;

            m_IsPopupOpened = true;
            FlowManager.Instance.PopupS?.Open<PopupDeath, BattleFlow>(EPopupType.PopupDeath, Flow);
        }
    }

    // 시장이 열린 동안 게임 시간을 멈춘다. 시장 팝업은 MarketClosed를 받으면 스스로 닫힌다
    private sealed class MarketState : FlowStateBase<BattleFlow>
    {
        public MarketState(BattleFlow flow) : base(flow) { }

        public override void Enter()
        {
            Flow.UnitS.StopBrains(); // 시장부터는 싸우지 않는다
            
            TimeManager.Instance.SetPause(EPauseReason.Market, true);
            SEventManager.Subscribe<MarketClosed>(OnMarketClosed);

            BattleService battleS = Flow.BattleS;
            PlayerDataManager.Instance.Hunt.BeginMarket(Flow.LootS.DicLoot, battleS.RemainTime, battleS.NightTime);
            FlowManager.Instance.PopupS?.Open<PopupMarket, BattleFlow>(EPopupType.PopupMarket, Flow);
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
            Flow.LootS.SetMarketReward(evt);
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
            Flow.UnitS.StopBrains();
            Flow.LootS.Settle(Flow.HuntResult); // 보상을 넣고
            Flow.BattleS.Finish(Flow.HuntResult); // 클리어 표시 후 저장
            m_Popup = FlowManager.Instance.PopupS?.Open<PopupResult, BattleFlow>(EPopupType.PopupResult, Flow);
        }

        public override void Exit()
        {
            if (m_Popup != null)
                m_Popup.Close();

            m_Popup = null;
        }
    }
}
