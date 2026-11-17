#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// 개발용 치트 패널(IMGUI). 오른쪽 위 DBG 버튼으로 연다. 릴리스 빌드에는 들어가지 않는다.
// 흐름 이동, 데이터베이스 아이템 지급, 세이브 저장·삭제를 한 곳에서 한다
public class DebugPanel : MonoBehaviour
{
    private const float RefHeight = 960f;

    private static readonly EGameState[] s_MoveStates = { EGameState.Lobby, EGameState.Battle, EGameState.Result };

    private bool m_IsOpen = false;
    private Vector2 m_ScrollPos = Vector2.zero;

    // ==================================================

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
        DrawItems(gameM);
        GUILayout.Space(8);
        DrawSave(gameM);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawFlow(GameManager gameM)
    {
        GUILayout.Label($"상태: {gameM.FlowService.Current}");
        GUILayout.BeginHorizontal();

        foreach (EGameState state in s_MoveStates)
        {
            GUI.enabled = gameM.FlowService.CanChange(state);
            if (GUILayout.Button(state.ToString()))
                gameM.FlowService.ChangeState(state);
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawItems(GameManager gameM)
    {
        GUILayout.Label("아이템 지급");
        if (gameM.Database == null)
            return;

        Inventory inventory = gameM.Player.Inventory;
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

    private void DrawSave(GameManager gameM)
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("저장"))
            gameM.RunSave();

        if (GUILayout.Button("세이브 삭제"))
            gameM.ResetSave();

        GUILayout.EndHorizontal();
    }
}
#endif
