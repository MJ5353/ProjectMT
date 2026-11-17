using MushTaker.Data;
using MushTaker.DevTools;
using MushTaker.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MushTaker.Core
{
    /// <summary>
    /// 게임 전역 진입점. 어떤 씬에서 플레이를 눌러도 씬 로드 전에 자동 생성된다.
    /// 데이터베이스, 플레이어 데이터, 세이브, 게임 흐름을 들고 있는다.
    /// </summary>

    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameDatabase Database { get; private set; }
        public PlayerData Player { get; private set; }
        public GameFlow Flow { get; private set; }

        private SaveService _save;

        // ==================================================
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Database = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
            if (Database == null)
                Debug.LogError($"[GameManager] Resources/{GameDatabase.ResourcePath} 데이터베이스가 없습니다. 메뉴 MushTaker/Data/데이터베이스 갱신을 실행하세요.");
            else
                Database.Initialize();

            _save = new SaveService();
            Player = new PlayerData();
            Player.LoadFrom(_save.Load());

            Flow = new GameFlow(this);
        }

        void Start()
        {
            Flow.ChangeState(GameFlow.StateForScene(SceneManager.GetActiveScene().name));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            go.AddComponent<DebugPanel>();
#endif
        }
        
        // save

        public void SaveNow() => _save.Save(Player.ToSaveData());

        public void ResetSave()
        {
            _save.Delete();
            Player.LoadFrom(new SaveData());
        }

        #region ApplicationEvent
        
        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();
        
        #endregion
    }
}
