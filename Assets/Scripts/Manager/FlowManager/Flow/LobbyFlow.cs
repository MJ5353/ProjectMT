// ==================================================

// 로비 씬 흐름. 지금은 상태 없이 공용 Service만 쓴다. 로비 전용 Service가 생기면 생성자에서 Register하고, 단계가 생기면 FlowBase<TState>로 바꾼다

public sealed class LobbyFlow : FlowBase
{
}
