
// ==================================================

// 팝업이 아닌 UI 요소(목록 칸·줄·아이콘·머리 위 바) 종류. UIReuseService·UIListView가 이 ID로 원본을 찾는다.
// 프리팹 이름은 enum 이름과 같게 둔다
public enum EUIComponentType
{
    None,

    // ui component
    UICommonSlot,
    
    // popup
    PopupMarket_MerchantRow,
    
    // screen
    ScreenBattle_LootRow,
    ScreenBattle_SkillIcon,
    ScreenBattle_UnitHpBar, // UIGauge
}

// ==================================================

// UI 요소 ID → 프리팹 표. Resources/Data/UI/UIComponent.asset 하나만 두고 UIReuseService가 읽는다.
// 타입이 아니라 ID로 찾으므로 같은 컴포넌트(UIGauge 등)로 프리팹을 여러 개 둘 수 있다.
// 새 요소는 enum에 한 줄 추가하고 같은 이름의 프리팹을 만든 뒤 "모든 UI 다시 만들기"를 돌리면 등록된다

public class UIComponentDef : UIPrefabDef<EUIComponentType, PoolableMonoUI>
{
    public const string UIComponentID = "UIComponent";
}
