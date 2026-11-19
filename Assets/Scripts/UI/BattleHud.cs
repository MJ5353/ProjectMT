using UnityEngine;

// 1단계 시험용 HUD(IMGUI). 체력, 처치 수, 주운 버섯. 결과 창은 UI 쪽 ResultScreen
// !!) 모바일 UI(uGUI 조이스틱·버튼)로 바꿀 때 지운다
public class BattleHud : MonoBehaviour
{
    private const float RefHeight = 960f;
    private GUIStyle m_LabelStyle;

    // ------------------------------------------------

    private void OnGUI()
    {
        BattleService battleM = GameManager.Instance?.BattleS;
        if (battleM == null)
            return;

        InitStyle();
        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        DrawStatus(battleM);
    }

    private void InitStyle()
    {
        if (m_LabelStyle != null)
            return;

        m_LabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22 };
    }

    private void DrawStatus(BattleService battleM)
    {
        GUILayout.BeginArea(new Rect(16, 16, 360, 260), GUI.skin.box);

        PlayerController player = battleM.Player;
        if (player != null)
            GUILayout.Label($"HP {player.HealthF.Current} / {player.HealthF.MaxHp}", m_LabelStyle);

        GUILayout.Label($"처치 {battleM.KillCount} / {battleM.KillGoal}", m_LabelStyle);
        foreach (var pair in battleM.DicLoot)
        {
            GUILayout.Label($"{GetItemName(pair.Key)} x{pair.Value}", m_LabelStyle);
        }

        GUILayout.Label("WASD 이동 · Shift 대시 · 클릭/스페이스 공격", GUI.skin.label);
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
