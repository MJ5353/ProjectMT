
// ==================================================

// 씬에서 도는 서비스. 공용은 FlowManager가, 씬 전용은 Flow(FlowBase)가 Init·Tick·Clear를 부른다.

public abstract class ServiceBase
{
    public virtual void Init() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Clear() { }
}
