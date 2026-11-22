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

    // 지금 씬을 다시 불러온다(전투 다시 하기)
    public bool ReloadScene()
    {
        // [mj] todo) 묶음 4에서 씬을 다시 읽지 않고 BattleFlow만 새로 시작하도록 바꾼다
        if (IsTransitioning)
            return false;

        return StartTransition(SceneManager.GetActiveScene().name);
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

        StartCoroutine(Transition(sceneName));
        return true;
    }

    private IEnumerator Transition(string sceneName)
    {
        IsTransitioning = true;

        try
        {
            UIManager uiM = UIManager.Instance;
            uiM.ShowLoading(true);

            // 씬 오브젝트가 살아 있을 때 지금 Flow와 Service를 정리한다(UI 칸 회수·팝업 삭제 포함)
            FlowManager.Instance.End();

            yield return SceneLoader.Load(sceneName, Define.MinLoadingTime, uiM.SetLoadingProgress);
        }
        finally
        {
            UIManager.Instance.ShowLoading(false);
            IsTransitioning = false;
        }
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
