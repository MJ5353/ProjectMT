
// GameManager가 씬의 서비스를 등록하고 수명 주기를 호출한다.
public abstract class ServiceBase
{
    public virtual void Init() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Clear() { }
}
