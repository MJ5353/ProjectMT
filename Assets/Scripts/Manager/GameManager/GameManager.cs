using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================

// 씬 전환을 맡는다(SceneDef로 씬 이름을 찾아 로딩 화면을 덮고 불러온다). 전역 데이터(Database·Define)도 여기서 읽는다.
// 씬 안의 흐름과 Service는 FlowManager가 맡는다. 씬을 바꾸기 전에 FlowManager.End로 지금 씬을 정리한다

public class GameManager : MonoSingleton<GameManager>
{
    private const string SceneDefMissingHint = "데이터 편집기에서 DB 갱신을 실행하세요.";
    
    private readonly Dictionary<ESceneType, SceneDef> m_DicScene = new();

    public GameDatabase Database { get; private set; }
    public DefineDef Define { get; private set; } // 전역 조절값. 에셋이 없으면 코드 기본값으로 만든다
    public bool IsTransitioning { get; private set; }

    // ------------------------------------------------

    // 지금 넘어갈 수 있는지(전환 중이 아니고, 씬이 등록돼 있고, 빌드에 들어 있다)
    public bool CanChangeScene(ESceneType type)
    {
        return IsTransitioning == false && CanLoad(type);
    }

    // 씬이 등록돼 있고 빌드에 들어 있는지(전환 중이어도 본다)
    public bool CanLoad(ESceneType type)
    {
        return TryGetSceneName(type, out string sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
    }

    public bool ChangeScene(ESceneType type)
    {
        if (IsTransitioning)
            return false;

        if (TryGetSceneName(type, out string sceneName) == false)
        {
            CLogger.Warning("GameManager", $"{type} 씬 정의(SceneDef)가 없습니다. {SceneDefMissingHint}");
            return false;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            CLogger.Warning("GameManager", $"이미 {sceneName} 씬입니다. 다시 불러오려면 ReloadScene을 쓰세요.");
            return false;
        }

        return StartTransition(sceneName);
    }

    // 지금 씬을 다시 불러온다(개발용 DebugPanel). 전투 다시 하기는 RestartFlow
    public bool ReloadScene()
    {
        if (IsTransitioning)
            return false;

        return StartTransition(SceneManager.GetActiveScene().name);
    }

    // 씬을 다시 읽지 않고 지금 Flow만 다시 시작한다(전투 다시 하기). 정리·재스폰이 보이지 않게 로딩 화면을 덮는다
    public bool RestartFlow()
    {
        if (IsTransitioning || FlowManager.Instance.IsRunning == false)
            return false;

        StartCoroutine(Loading(RestartRoutine()));
        return true;
    }

    private bool TryGetSceneName(ESceneType type, out string sceneName)
    {
        sceneName = null;
        if (m_DicScene.TryGetValue(type, out SceneDef def) == false || def.SceneName.E_IsNullOrEmpty())
            return false;

        sceneName = def.SceneName;
        return true;
    }

    private bool StartTransition(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName) == false)
        {
            CLogger.Warning("GameManager", $"씬을 로드할 수 없습니다(Build Settings 확인): {sceneName}");
            return false;
        }

        StartCoroutine(Loading(LoadSceneRoutine(sceneName)));
        return true;
    }

    // 로딩 화면을 덮은 채 work를 돌린다. 도는 동안은 전환 중(IsTransitioning)이라 다른 전환을 받지 않는다
    private IEnumerator Loading(IEnumerator work)
    {
        IsTransitioning = true;

        try
        {
            UIManager.Instance.ShowLoading(true);
            yield return work;
        }
        finally
        {
            UIManager.Instance.ShowLoading(false);
            IsTransitioning = false;
        }
    }

    // 씬 오브젝트가 살아 있을 때 지금 Flow와 Service를 정리하고(UI 칸 회수·팝업 삭제 포함) 다음 씬을 읽는다
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        FlowManager.Instance.End();
        yield return SceneLoader.Load(sceneName, Define.Loading.MinLoadingTime, UIManager.Instance.SetLoadingProgress);
    }

    // 누른 버튼(결과 팝업) 처리가 끝난 다음 프레임에 Flow를 다시 돌리고, 씬 전환과 같은 시간만큼 덮는다
    private IEnumerator RestartRoutine()
    {
        yield return null;

        FlowManager.Instance.Restart();
        yield return SceneLoader.Wait(Define.Loading.MinLoadingTime, UIManager.Instance.SetLoadingProgress);
    }

    // virtual ---

    protected override void OnInit()
    {
        Database = ResourceManager.Instance.Load<GameDatabase>(CommonPath.GameDatabase, "MushTaker/데이터 편집기에서 DB 갱신을 실행하세요.");
        Database.Init();

        // !!) 아직 Instance 등록 전이라 GameDatabase.S_TryGet이 아닌 Database.TryGet
        if (Database.TryGet(DefineDef.DefineID, out DefineDef define) == false)
        {
            CLogger.Warning("GameManager", "Define 에셋이 DB에 없어 코드 기본값을 씁니다.");
            define = ScriptableObject.CreateInstance<DefineDef>();
        }

        Define = define;

        m_DicScene.Clear();
        foreach (SceneDef sceneDef in Database.GetAll<SceneDef>())
        {
            if (sceneDef == null || m_DicScene.TryAdd(sceneDef.Type, sceneDef))
                continue;

            CLogger.Warning("GameManager", $"같은 씬 종류가 두 번 등록됨: {sceneDef.Type} ({sceneDef.name})");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (TryGetComponent<DebugPanel>(out _) == false)
            gameObject.AddComponent<DebugPanel>();
#endif
    }
}
