using MushTaker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MushTaker.UI
{
    /// 로비에서 전투로 들어가는 버튼.
    [RequireComponent(typeof(Button))]
    public class StartBattleButton : MonoBehaviour
    {
        void Awake() => GetComponent<Button>().onClick.AddListener(() => GameManager.Instance.Flow.ChangeState(GameState.Battle));
    }
}
