using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 쓰러졌을 때 뜨는 팝업(BattleFlow Down 상태가 Flow를 넘겨 띄운다). 골드를 내고 부활하거나 빈손으로 귀가한다. Down 동안 밤 시계는 멈춘다

public class PopupDeath : PopupFlowBase<BattleFlow>
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
        BattleService battleS = Flow.BattleS;
        m_ReviveLabel.E_SetString(EStringKey.Death_ReviveFormat, battleS.ReviveGold);
        m_ReviveButton.E_SetInteractable(battleS.CanRevive); // 골드가 모자라면 못 누른다
    }

    // ---

    private void OnClickRevive()
    {
        if (Flow.TryRevive())
            Close();
    }

    // 먼저 닫고 귀가한다(결과 팝업이 이 위에 뜬다)
    private void OnClickHome()
    {
        BattleFlow battle = Flow;
        Close();
        battle.GiveUp();
    }
}
