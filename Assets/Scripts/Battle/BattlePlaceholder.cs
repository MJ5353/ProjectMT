using MushTaker.Core;
using UnityEngine;

namespace MushTaker.Battle
{
    /// <summary>
    /// 전투 기획이 정해지기 전 흐름 확인용 임시 전투. 승리 → 결과 → 로비 이동만 한다.
    /// 실제 전투가 들어오면 지운다.
    /// </summary>
    public class BattlePlaceholder : MonoBehaviour
    {
        [SerializeField] string rewardId = "Currency_Gold";
        [SerializeField] int rewardAmount = 10;

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            float scale = Screen.height / 960f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var area = new Rect(Screen.width / scale / 2 - 100, 400, 200, 120);

            GUILayout.BeginArea(area, GUI.skin.box);
            if (gm.Flow.Current == GameState.Battle)
            {
                GUILayout.Label("전투 중 (임시)");
                if (GUILayout.Button("승리", GUILayout.Height(40)))
                {
                    gm.Player.Inventory.Add(rewardId, rewardAmount);
                    gm.Flow.ChangeState(GameState.Result);
                }
            }
            else if (gm.Flow.Current == GameState.Result)
            {
                GUILayout.Label($"결과: {rewardId} +{rewardAmount}");
                if (GUILayout.Button("로비로", GUILayout.Height(40))) gm.Flow.ChangeState(GameState.Lobby);
            }
            GUILayout.EndArea();
        }
    }
}
