using System;

// ==================================================

// 지금 씬의 흐름(Flow)과 모든 씬이 같이 쓰는 공용 Service(이펙트·UI 요소·팝업)를 맡는다.
// 공용 Service는 처음 한 번 만들어 계속 들고, 씬마다 Begin에서 연결(Init)하고 End에서 해제(Clear)한다.
// 씬 전용 Service는 Flow가 들고(BattleFlow → Unit·HitStop·Battle), 씬 전환은 GameManager가 맡는다.
// 접근: FlowManager.Instance.EffectS / UIReuseS / PopupS, 전투 전용은 FlowManager.Instance.Battle?.UnitS 처럼.
// !!) 공용 Service 접근자는 Flow가 돌지 않는 동안(씬 전환 사이) null이다. 부르는 쪽은 ?.로 거른다

public sealed class FlowManager : MonoSingleton<FlowManager>
{
    public FlowBase Current { get; private set; }
    
    private Func<FlowBase> m_CreateFlow; // 지금 씬의 Flow를 만드는 함수(씬 컨트롤러 CreateFlow). 다시 하기(Restart)가 같은 Flow를 새로 만든다
    private EffectService m_EffectS;
    private UIReuseService m_UIReuseS;
    private PopupService m_PopupS;
    
    public bool IsRunning => Current != null;
    public BattleFlow Battle => Current as BattleFlow; // 지금 Flow가 전투일 때만 있다
    public EffectService EffectS => IsRunning ? m_EffectS : null;
    public UIReuseService UIReuseS => IsRunning ? m_UIReuseS : null;
    public PopupService PopupS => IsRunning ? m_PopupS : null;

    // ------------------------------------------------

    // 씬 컨트롤러가 Start에서 Flow를 만드는 함수와 함께 부른다. 이전 Flow가 남아 있으면 먼저 정리한다
    // !!) 이펙트를 맨 먼저 연결한다. 전투 Service가 Init에서 이펙트를 부를 수 있다
    public void Begin(Func<FlowBase> createFlow)
    {
        End();

        FlowBase flow = createFlow?.Invoke();
        if (flow == null)
            return;

        m_CreateFlow = createFlow;
        Current = flow;
        
        m_EffectS.Init();
        m_UIReuseS.Init();
        m_PopupS.Init();

        flow.Begin();
        SEventManager.Publish(new FlowBegan(flow));
    }

    // 씬은 그대로 두고 지금 Flow만 정리한 뒤 같은 Flow를 새로 시작한다(전투 다시 하기, GameManager.RestartFlow가 부른다).
    // 공용 Service는 연결을 유지하고(화면이 들고 있는 UI 요소·팝업은 그대로), 월드에 남은 이펙트만 비운다
    public bool Restart()
    {
        if (IsRunning == false)
            return false;

        FlowBase flow = m_CreateFlow?.Invoke();
        if (flow == null)
            return false;

        Current.End();
        m_EffectS.Clear();

        Current = flow;
        flow.Begin();
        
        SEventManager.Publish(new FlowBegan(flow));
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (IsRunning == false)
            return;

        m_EffectS.Tick(deltaTime);
        m_UIReuseS.Tick(deltaTime);
        m_PopupS.Tick(deltaTime);
        
        Current.Tick(deltaTime);
    }

    // 씬을 바꾸기 전에 정리하는 입구. 씬 오브젝트가 아직 살아 있을 때 부른다(GameManager가 로드 전에, 씬 컨트롤러가 OnDestroy에서 한 번 더)
    // !!) 순서: 씬 전용 Service → UI 요소 → 팝업 → 이펙트. 팝업이 지워지기 전에 팝업 안 칸을 먼저 회수해야 같이 지워지지 않는다
    // Current는 맨 끝에 비운다. 정리 중에도 Battle·PopupS 접근이 된다
    public void End()
    {
        if (IsRunning == false)
            return;

        Current.End();
        m_UIReuseS.Clear();
        m_PopupS.Clear();
        m_EffectS.Clear();
        
        Current = null;
        m_CreateFlow = null;
    }

    // virtual ---

    protected override void OnInit()
    {
        m_EffectS = new EffectService();
        m_UIReuseS = new UIReuseService();
        m_PopupS = new PopupService();
    }

    protected override void OnClear()
    {
        End();
    }
}
