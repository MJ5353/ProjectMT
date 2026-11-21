using System.Collections.Generic;
using UnityEngine;

// ==================================================

// ResourceManager를 감싸 UI 요소를 꺼내고 돌려준다(UnitFactory와 같은 역할). 씬마다 하나, GameManager가 공용 Service로 넣는다.
// 꺼낸 요소를 기억해 두었다가 씬이 바뀌기 직전(SceneUnloading)에 한꺼번에 풀로 돌려받는다. 화면 쪽은 씬 전환 때 따로 반납하지 않는다.
// !!) GameManager가 PopupService보다 먼저 등록해서, 팝업이 지워지기 전에 팝업 안 칸부터 회수한다

public sealed class UIFactory : ServiceBase
{
    private readonly List<PoolableMonoUI> m_ListSpawned = new();
    private UIComponentDef m_Def;

    // ------------------------------------------------

    private void OnSceneUnloading(SceneUnloading evt) => DespawnAll();
    
    public override void Init()
    {
        if (GameDatabase.S_TryGet(UIComponentDef.UIComponentID, out m_Def) == false)
            CLogger.Error("UIFactory", "UIComponentDef가 DB에 없습니다. UI 프리팹 표 갱신을 실행하세요.");
        
        SEventManager.Subscribe<SceneUnloading>(OnSceneUnloading);
    }

    public override void Clear()
    {
        SEventManager.Unsubscribe<SceneUnloading>(OnSceneUnloading);
        DespawnAll();
        
        m_Def = null;
    }

    public T Spawn<T>(EUIComponentType type, Transform parent) where T : PoolableMonoUI
    {
        T prefab = m_Def.E_Alive()?.Get<T>(type);
        if (prefab == null)
            return null;

        // 원본(UIComponentDef)을 parent 아래로 꺼낸다. 원본이 없거나 T가 맞지 않으면 null(에러는 UIComponentDef가 한 번)
        T item = ResourceManager.Instance.Spawn(prefab, parent);
        if (item != null)
            m_ListSpawned.Add(item);

        return item;
    }

    public void Despawn(PoolableMonoUI item)
    {
        if (item == null)
            return;

        m_ListSpawned.E_RemoveBack(item);
        ResourceManager.S_Despawn(item);
    }

    // 꺼내 준 요소를 모두 돌려받는다. 씬과 함께 이미 지워진 것은 건너뛴다(ResourceManager.Despawn이 null을 거른다)
    public void DespawnAll()
    {
        foreach (PoolableMonoUI spawned in m_ListSpawned)
        {
            ResourceManager.S_Despawn(spawned);
        }
        
        m_ListSpawned.Clear();
    }
}
