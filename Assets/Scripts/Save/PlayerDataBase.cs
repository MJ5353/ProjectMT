// ==================================================

// 플레이어 데이터(InventoryData, FlagData, HuntData 등)의 기반.
// PlayerDataManager가 만들어 등록하고 Init, Clear, 세이브 읽기·쓰기를 순회로 한 번에 돌린다
public abstract class PlayerDataBase
{
    // virtual ---

    public virtual void Init() { }                  // 만든 직후 한 번
    public virtual void Clear() { }                 // 세이브 삭제, 매니저 정리
    public virtual void Load(SaveData data) { }     // 세이브에서 읽기. 이벤트를 발행하지 않는다
    public virtual void Save(SaveData data) { }     // 세이브에 쓰기
}
