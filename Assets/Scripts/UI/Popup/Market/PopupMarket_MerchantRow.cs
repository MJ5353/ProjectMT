using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 시장 왼쪽 상인 목록 한 줄. 이름, 신뢰도 숫자·게이지, 고른 상태 테두리

public class PopupMarket_MerchantRow : PoolableMonoUI
{
    [SerializeField] private Button m_Button = null;
    [SerializeField] private TMP_Text m_NameText = null;
    [SerializeField] private TMP_Text m_TrustText = null; // "신뢰 25". 없어도 된다
    [SerializeField] private UIGauge m_Trust = null;
    [SerializeField] private GameObject m_Selected = null;

    private Action<PopupMarket_MerchantRow> m_OnClick;

    // ------------------------------------------------

    private void Awake()
    {
        m_Button.E_SetOnClick(() => m_OnClick?.Invoke(this));
    }

    public void Set(MerchantDef merchant, int trust, int trustMax, bool isSelected)
    {
        m_NameText.E_SetText(merchant.DisplayName);
        m_Trust.SetValue(trust, trustMax, true);
        m_TrustText.E_SetString(EStringKey.Market_Row_TrustFormat, trust);
        m_Selected.E_SetActive(isSelected);
    }

    public void SetOnClick(Action<PopupMarket_MerchantRow> onClick)
    {
        m_OnClick = onClick;
    }
}
