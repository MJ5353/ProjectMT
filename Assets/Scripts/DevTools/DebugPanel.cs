#if UNITY_EDITOR || DEVELOPMENT_BUILD
using MushTaker.Core;
using MushTaker.Data;
using UnityEngine;

namespace MushTaker.DevTools
{
    /// <summary>
    /// 개발용 치트 패널(IMGUI). 오른쪽 위 DBG 버튼으로 연다. 릴리스 빌드에는 들어가지 않는다.
    /// 흐름 이동, 데이터베이스 아이템 지급, 세이브 저장·삭제를 한 곳에서 한다.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        const float RefHeight = 960f;

        bool _open;
        Vector2 _scroll;

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // 해상도와 상관없이 비슷한 크기로 보이게 높이 기준으로 확대한다.
            float scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;

            if (GUI.Button(new Rect(w - 70, 10, 60, 30), _open ? "닫기" : "DBG")) _open = !_open;
            if (!_open) return;

            GUILayout.BeginArea(new Rect(w - 290, 50, 280, RefHeight - 60), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label($"상태: {gm.Flow.Current}");
            GUILayout.BeginHorizontal();
            foreach (GameState s in new[] { GameState.Lobby, GameState.Battle, GameState.Result })
            {
                GUI.enabled = gm.Flow.CanChange(s);
                if (GUILayout.Button(s.ToString())) gm.Flow.ChangeState(s);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("아이템 지급");
            if (gm.Database != null)
            {
                foreach (var def in gm.Database.All)
                {
                    if (def == null) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{def.DisplayName} ({gm.Player.Inventory.GetCount(def.Id)})", GUILayout.Width(170));
                    if (GUILayout.Button("+1")) gm.Player.Inventory.Add(def.Id, 1);
                    if (GUILayout.Button("+10")) gm.Player.Inventory.Add(def.Id, 10);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("저장")) gm.SaveNow();
            if (GUILayout.Button("세이브 삭제")) gm.ResetSave();
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
#endif
