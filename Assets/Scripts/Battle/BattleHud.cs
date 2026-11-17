using UnityEngine;

// 1단계 시험용 HUD(IMGUI). 체력, 처치 수, 주운 버섯, 결과 창.
// !!) 모바일 UI(uGUI 조이스틱·버튼)로 바꿀 때 지운다
public class BattleHud : MonoBehaviour
{
    private const float RefHeight = 960f;

    private GUIStyle m_LabelStyle;
    private GUIStyle m_TitleStyle;

    // ==================================================

    private void OnGUI()
    {
        BattleManager battleM = BattleManager.Instance;
        if (battleM == null)
            return;

        InitStyle();
        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        DrawStatus(battleM);
        if (battleM.State != EBattleState.Playing)
            DrawResult(battleM, width);
    }

    private void InitStyle()
    {
        if (m_LabelStyle != null)
            return;

        m_LabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        m_TitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
    }

    private void DrawStatus(BattleManager battleM)
    {
        GUILayout.BeginArea(new Rect(16, 16, 360, 260), GUI.skin.box);

        PlayerController player = battleM.Player;
        if (player != null)
            GUILayout.Label($"HP {player.Health.Current} / {player.Health.MaxHp}", m_LabelStyle);

        GUILayout.Label($"처치 {battleM.KillCount} / {battleM.KillGoal}", m_LabelStyle);
        foreach (var pair in battleM.DicLoot)
        {
            GUILayout.Label($"{GetItemName(pair.Key)} x{pair.Value}", m_LabelStyle);
        }

        GUILayout.Label("WASD 이동 · Shift 대시 · 클릭/스페이스 공격", GUI.skin.label);
        GUILayout.EndArea();
    }

    private void DrawResult(BattleManager battleM, float width)
    {
        GUILayout.BeginArea(new Rect(width / 2f - 180f, 300f, 360f, 300f), GUI.skin.box);
        GUILayout.Label(battleM.State == EBattleState.Won ? "사냥 성공" : "쓰러졌다", m_TitleStyle, GUILayout.Height(70));
        GUILayout.Label($"시간 {battleM.ElapsedTime:0}초 · 처치 {battleM.KillCount}", m_LabelStyle);

        foreach (var pair in battleM.DicLoot)
        {
            GUILayout.Label($"{GetItemName(pair.Key)} +{pair.Value}", m_LabelStyle);
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("다시 하기", GUILayout.Height(48)))
            battleM.Retry();
        if (Application.CanStreamedLevelBeLoaded(SceneNames.Lobby) && GUILayout.Button("로비로", GUILayout.Height(48)))
            battleM.TryGoLobby();

        GUILayout.EndArea();
    }

    private static string GetItemName(string itemID)
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null || gameM.Database == null)
            return itemID;
        if (gameM.Database.TryGet(itemID, out MushroomDef def) == false)
            return itemID;

        return def.DisplayName;
    }
}
