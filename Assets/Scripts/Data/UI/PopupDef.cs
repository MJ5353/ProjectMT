// ==================================================

// 팝업 종류. 팝업 ID로 쓴다(PopupService.Open<T>(EPopupType)). 프리팹 이름은 enum 이름과 같게 둔다
public enum EPopupType
{
    None,
    PopupDeath,
    PopupResult,
    PopupMarket,
    PopupMarketCounter,
    PopupConfirm,
}

// ==================================================

// 팝업 ID → 팝업 프리팹 표. Resources/Data/UI/Popup.asset 하나만 두고 PopupService가 읽는다.
// 새 팝업은 enum에 한 줄 추가하고 같은 이름의 프리팹을 만든 뒤 "모든 UI 다시 만들기"를 돌리면 등록된다

public class PopupDef : UIPrefabDef<EPopupType, PopupBase>
{
    public const string PopupID = "Popup";
}
