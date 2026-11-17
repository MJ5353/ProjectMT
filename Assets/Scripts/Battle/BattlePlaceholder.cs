using UnityEngine;

// 전투 기획이 정해지기 전 흐름 확인용 임시 전투. 승리 → 결과 → 로비 이동만 한다.
// !!) 실제 전투가 들어오면 지울 것
public class BattlePlaceholder : MonoBehaviour
{
    private const float RefHeight = 960f;

    [SerializeField] private string m_RewardID = "Currency_Gold";
    [SerializeField] private int m_RewardAmount = 10;

    // ==================================================

    private void OnGUI()
    {
        GameManager gameM = GameManager.Instance;
        if (gameM == null)
            return;

        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        Rect area = new Rect(Screen.width / scale / 2 - 100, 400, 200, 120);
        GUILayout.BeginArea(area, GUI.skin.box);

        switch (gameM.Flow.Current)
        {
            case EGameState.Battle:
                DrawBattle(gameM);
                break;
            case EGameState.Result:
                DrawResult(gameM);
                break;
        }

        GUILayout.EndArea();
    }

    private void DrawBattle(GameManager gameM)
    {
        GUILayout.Label("전투 중 (임시)");

        if (GUILayout.Button("승리", GUILayout.Height(40)) == false)
            return;

        gameM.Player.Inventory.Add(m_RewardID, m_RewardAmount);
        gameM.Flow.ChangeState(EGameState.Result);
    }

    private void DrawResult(GameManager gameM)
    {
        GUILayout.Label($"결과: {m_RewardID} +{m_RewardAmount}");

        if (GUILayout.Button("로비로", GUILayout.Height(40)))
            gameM.Flow.ChangeState(EGameState.Lobby);
    }
}
