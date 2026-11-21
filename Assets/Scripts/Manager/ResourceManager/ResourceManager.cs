using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================

// PoolableMono를 만들고, 돌려받고, 다시 꺼내 쓴다. 프리팹마다 꺼 둔 인스턴스를 쌓아 둔다.
// Resources 에셋 로드(Load)도 여기서 한 번만 하고 캐시한다. 경로는 CommonPath.
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneControllerBase가 Create).
// 돌려받은 인스턴스는 이 오브젝트 아래(Pool)에 보관해서 씬이 바뀌어도 다시 쓴다

public class ResourceManager : MonoSingleton<ResourceManager>
{
    private const string PoolRootName = "Pool";
    private Transform m_PoolRoot;
    
    private readonly Dictionary<int, Stack<PoolableMono>> m_DicPool = new();   // 프리팹 InstanceID → 꺼 둔 인스턴스
    private readonly Dictionary<string, Object> m_DicLoaded = new();          // Resources 경로 → 로드한 에셋(없으면 null로 남겨 에러를 한 번만)

    // ------------------------------------------------

    // Resources에서 한 번만 로드해 캐시한다. 없으면 null(에러는 경로마다 한 번)
    public T Load<T>(string resourcePath, string missingHint = null) where T : Object
    {
        if (m_DicLoaded.TryGetValue(resourcePath, out Object cached))
            return cached as T;

        T asset = Resources.Load<T>(resourcePath);
        m_DicLoaded.Add(resourcePath, asset);
        
        if (asset == null)
            CLogger.Error("ResourceManager", $"Resources/{resourcePath} 에셋이 없습니다. {missingHint}");

        return asset;
    }

    // 풀에 있으면 꺼내 켜고, 없으면 새로 만든다. 켜기 전에 위치를 옮겨서 CharacterController가 덮어쓰지 않게 한다
    public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : PoolableMono
    {
        if (prefab == null)
            return null;

        int poolKey = prefab.GetInstanceID();
        T instance = Pop(poolKey) as T;
        
        if (instance == null)
        {
            instance = Instantiate(prefab, position, rotation);
            instance.SetPoolKey(poolKey);
        }
        else
        {
            // 보관 중에는 [Managers](DontDestroyOnLoad) 아래에 있으니 지금 씬으로 다시 옮긴다
            instance.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(instance.gameObject, SceneManager.GetActiveScene());
            
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
        }

        instance.OnPoolSpawn();
        return instance;
    }

    // UI용. parent 아래로 꺼낸다(씬 이동 없이 부모만 바꾼다). 위치는 PoolableMonoUI.OnPoolSpawn이 원복한다
    public T Spawn<T>(T prefab, Transform parent) where T : PoolableMono
    {
        if (prefab == null)
            return null;

        int poolKey = prefab.GetInstanceID();
        T instance = Pop(poolKey) as T;

        if (instance == null)
        {
            instance = Instantiate(prefab, parent, false);
            instance.SetPoolKey(poolKey);
        }
        else
        {
            instance.transform.SetParent(parent, false);
        }

        instance.gameObject.SetActive(true); // 원본 프리팹이 꺼져 있어도 켠다
        instance.OnPoolSpawn();
        return instance;
    }

    // 꺼서 풀에 넣는다. ResourceManager가 만든 것이 아니면 지운다
    public void Despawn(PoolableMono instance)
    {
        if (instance == null)
            return;

        if (instance.PoolKey == 0)
        {
            Delete(instance.gameObject);
            return;
        }

        instance.OnPoolDespawn();
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(m_PoolRoot, false);

        if (m_DicPool.TryGetValue(instance.PoolKey, out Stack<PoolableMono> pool) == false)
        {
            pool = new Stack<PoolableMono>();
            m_DicPool.Add(instance.PoolKey, pool);
        }

        pool.Push(instance);
    }

    // 풀 없이 새로 만든다(팝업·맵·부위 모델 등 다시 쓰지 않거나 따로 들고 있는 것). 오브젝트 생성은 여기로 모은다. 원본이 없으면 null
    public T Create<T>(T prefab, Transform parent = null) where T : Object
    {
        if (prefab == null)
            return null;

        return Instantiate(prefab, parent);
    }

    // 풀에 넣지 않고 지운다(팝업 인스턴스 등). 오브젝트 삭제는 여기로 모은다
    public void Delete(GameObject target)
    {
        if (target == null)
            return;

        Destroy(target);
    }

    // 씬과 함께 지워진 인스턴스는 건너뛴다
    private PoolableMono Pop(int poolKey)
    {
        if (m_DicPool.TryGetValue(poolKey, out Stack<PoolableMono> pool) == false)
            return null;

        while (pool.Count > 0)
        {
            PoolableMono instance = pool.Pop();
            if (instance != null)
                return instance;
        }

        return null;
    }

    // virtual ---

    protected override void OnInit()
    {
        m_PoolRoot = new GameObject(PoolRootName).transform;
        m_PoolRoot.SetParent(transform, false);
    }

    protected override void OnClear()
    {
        foreach (Stack<PoolableMono> pool in m_DicPool.Values)
        {
            foreach (PoolableMono instance in pool)
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }
        }

        m_DicPool.Clear();
        m_DicLoaded.Clear();
        
        if (m_PoolRoot != null)
            Destroy(m_PoolRoot.gameObject);
    }
    
    // static ---

    public static void S_Despawn(PoolableMono instance)
    {
        Instance?.Despawn(instance);
    }

    public static T S_Create<T>(T prefab, Transform parent = null) where T : Object
    {
        return Instance?.Create(prefab, parent);
    }

    public static void S_Delete(GameObject target)
    {
        Instance?.Delete(target);
    }
}
