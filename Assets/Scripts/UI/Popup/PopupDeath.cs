using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 쓰러졌을 때 뜨는 팝업. 골드를 내고 부활하거나 빈손으로 귀가한다. 떠 있는 동안 밤 시계는 멈춘다(BattleService)

public class PopupDeath : PopupBase
{
    [SerializeField] private Button m_ReviveButton = null;
    [SerializeField] private Button m_HomeButton = null;
    [SerializeField] private TMP_Text m_ReviveLabel = null;

    public override bool IsCloseOnBack => false; // 둘 중 하나는 골라야 한다

    // ------------------------------------------------

    private void Awake()
    {
        m_ReviveButton.E_SetOnClick(OnClickRevive);
        m_HomeButton.E_SetOnClick(OnClickHome);
    }

    // virtual ---

    protected override void OnOpen()
    {
        BattleService battleS = GameManager.Instance.BattleS;
        if (battleS == null)
        {
            // 다시 쓰는 팝업이라 지난번 값이 남지 않게 비우고 부활은 막는다
            m_ReviveLabel.E_SetText(string.Empty);
            m_ReviveButton.E_SetInteractable(false);
            return;
        }

        m_ReviveLabel.E_SetString(EStringKey.Death_ReviveFormat, battleS.ReviveGold);
        m_ReviveButton.E_SetInteractable(battleS.CanRevive); // 골드가 모자라면 못 누른다
    }

    // ---

    private void OnClickRevive()
    {
        BattleService battleS = GameManager.Instance.BattleS;
        if (battleS != null && battleS.TryRevive())
            Close();
    }

    private void OnClickHome()
    {
        BattleService battleS = GameManager.Instance.BattleS;
        Close();
        battleS?.GiveUp();
    }
}
