#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// 개발용 치트 패널(IMGUI). 오른쪽 위 DBG 버튼으로 연다. 릴리스 빌드에는 들어가지 않는다.
// 흐름 이동, 몬스터 소환, 데이터베이스 아이템 지급, 세이브 저장·삭제를 한 곳에서 한다
public class DebugPanel : MonoBehaviour
{
    private const float RefHeight = 960f;
    private const string DefaultMapID = "Map_001_Field"; // Battle 씬 단독 실행 때 쓰는 지역
    private const float SpawnGap = 2f; // 플레이어 캡슐과 소환 몬스터 캡슐 사이 거리(m)

    private static readonly EGameState[] s_MoveStates = { EGameState.Lobby, EGameState.Battle, EGameState.Result };

    private bool m_IsOpen = false;
    private Vector2 m_ScrollPos = Vector2.zero;

    // ------------------------------------------------

    private void OnGUI()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null)
            return;

        // 해상도와 상관없이 비슷한 크기로 보이게 높이 기준으로 확대한다
        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        if (GUI.Button(new Rect(width - 70, 10, 60, 30), m_IsOpen ? "닫기" : "DBG"))
            m_IsOpen = m_IsOpen == false;

        if (m_IsOpen == false)
            return;

        GUILayout.BeginArea(new Rect(width - 290, 50, 280, RefHeight - 60), GUI.skin.box);
        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);

        DrawFlow(gameM);
        GUILayout.Space(8);
        DrawSpawn(gameM);
        GUILayout.Space(8);
        DrawItems(gameM);
        GUILayout.Space(8);
        DrawSave();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawFlow(GameManager gameM)
    {
        GUILayout.Label($"상태: {gameM.Current}");
        GUILayout.BeginHorizontal();

        foreach (EGameState state in s_MoveStates)
        {
            GUI.enabled = gameM.CanChange(state);
            if (GUILayout.Button(state.ToString()))
                gameM.ChangeState(state);
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    // 전투 중일 때만 몬스터 목록을 보여 주고, 누르면 플레이어 앞에 소환한다
    private void DrawSpawn(GameManager gameM)
    {
        if (gameM.UnitS == null || gameM.UnitS.Player == null || gameM.Database == null)
            return;

        GUILayout.Label("몬스터 소환(플레이어 앞)");
        foreach (MonsterDef def in gameM.Database.GetAll<MonsterDef>())
        {
            if (def == null || def.Prefab == null)
                continue;

            if (GUILayout.Button(def.ID))
                SpawnInFront(gameM.UnitS, def);
        }
    }

    private static void SpawnInFront(UnitService units, MonsterDef def)
    {
        Transform player = units.Player.transform;
        float distance = SpawnGap + units.Player.HurtBody.radius;
        if (def.Prefab.TryGetComponent(out CharacterController controller))
            distance += controller.radius;

        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        Vector3 position = player.position + forward * distance + Vector3.up * BattleService.SpawnLift;
        units.Spawn(def.Prefab, position, Quaternion.LookRotation(-forward, Vector3.up));
    }

    public static MapDef GetDefaultMap(GameDatabase database)
    {
        if (database == null || database.TryGet(DefaultMapID, out MapDef map) == false)
            return null;

        CLogger.Warning("DebugPanel", $"고른 지역이 없어 개발용 기본 지역 {DefaultMapID}을 씁니다.");
        return map;
    }

    private void DrawItems(GameManager gameM)
    {
        GUILayout.Label("아이템 지급");
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (gameM.Database == null || playerDataM == null)
            return;

        InventoryData inventory = playerDataM.Inventory;
        foreach (DataDef def in gameM.Database.ListDef)
        {
            if (def == null)
                continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{def.DisplayName} ({inventory.GetCount(def.ID)})", GUILayout.Width(170));

            if (GUILayout.Button("+1"))
                inventory.Add(def.ID, 1);

            if (GUILayout.Button("+10"))
                inventory.Add(def.ID, 10);

            GUILayout.EndHorizontal();
        }
    }

    private void DrawSave()
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (playerDataM == null)
            return;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("저장"))
            playerDataM.RunSave();

        if (GUILayout.Button("세이브 삭제"))
            playerDataM.ResetSave();

        GUILayout.EndHorizontal();
    }
}
#endif
