// ==================================================

// BattleFlow의 상태들. 상태마다 Enter·Update·Exit에서 할 일만 두고, 상태를 넘기는 요청은 BattleFlow 함수(EndHunt 등)를 쓴다

public sealed partial class BattleFlow
{
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
            PlayerDataManager.Instance.Hunt.BeginMarket(battleS.DicLoot, battleS.RemainTime, battleS.NightTime);
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
            Flow.UnitS.StopBrains();
            Flow.BattleS.Finish(Flow.HuntResult);
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
