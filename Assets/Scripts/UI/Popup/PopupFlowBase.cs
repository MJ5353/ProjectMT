// ==================================================

// Flow에 붙는 팝업의 표시(PopupService가 Flow 없이 여는 것을 막을 때 본다). 직접 상속하지 않고 PopupFlowBase<TFlow>를 쓴다
public abstract class PopupFlowBase : PopupBase
{
}

// ==================================================

// 특정 Flow에 붙는 팝업(사망·결과·시장 → BattleFlow). 여는 쪽이 Flow를 넘겨야만 열린다(PopupService.Open<T, TFlow>(type, flow)).
// Flow는 OnOpen보다 먼저 들어오므로 OnOpen·여는 연출·버튼에서 Flow를 바로 쓴다. 어느 Flow에서 열렸는지 팝업이 찾지 않는다
// Flow가 없어도 되는 공용 팝업(확인·개수 조절)은 PopupBase를 쓰고, 필요한 값은 연 쪽이 Set으로 넣는다

public abstract class PopupFlowBase<TFlow> : PopupFlowBase where TFlow : FlowBase
{
    protected TFlow Flow { get; private set; }

    // ------------------------------------------------

    // PopupService만 부른다(Open 직전)
    public void SetFlow(TFlow flow) => Flow = flow;
}
