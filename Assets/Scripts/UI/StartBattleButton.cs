using UnityEngine;
using UnityEngine.UI;

// 로비에서 전투로 들어가는 버튼
[RequireComponent(typeof(Button))]
public class StartBattleButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClickStart);
    }

    private void OnClickStart()
    {
        GameManager.Instance.Flow.ChangeState(EGameState.Battle);
    }
}
