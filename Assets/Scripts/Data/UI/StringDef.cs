using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 화면 문구 키. 화면 접두로 묶는다(Common_·Lobby_·Battle_·Clock_·Death_·Result_ …). 숫자가 들어가는 문구는 Format으로 끝낸다.
// !!) 표(StringDef)와 UILabel은 키를 enum 이름 문자열로 저장한다. 순서는 바꾸거나 끼워 넣어도 되지만, 이름은 한 번 정하면 바꾸지 않는다
public enum EStringKey
{
    None,

    // common
    Common_GameTitle,
    Common_Yes,
    Common_No,
    Common_Cancel,
    Common_Confirm,

    // lobby
    Lobby_Tab_Gear,
    Lobby_Tab_Hunt,
    Lobby_Tab_Breed,
    Lobby_Tab_Dispatch,
    Lobby_ComingSoon,
    Lobby_Hunt_MapTitle,
    Lobby_Hunt_MushroomHeader,
    Lobby_Hunt_Enter,
    Lobby_Hunt_NoMap,
    Lobby_Hunt_NoMapDesc,
    Lobby_Hunt_Cleared,
    Lobby_Hunt_Unlocked,
    Lobby_Hunt_LockedFormat,    // {0} 먼저 토벌할 지역

    // lobby · gear (ScreenLobby_GearView)
    Lobby_Gear_TreeTitle,
    Lobby_Gear_EmblemProgressFormat, // {0} 연 노드 {1} 전체
    Lobby_Gear_EmblemNextFormat,    // {0} 남은 노드
    Lobby_Gear_EmblemMax,
    Lobby_Gear_EmblemBonusHeader,
    Lobby_Gear_NoBonus,
    Lobby_Gear_SelectHint,
    Lobby_Gear_RequireFormat,       // {0} 앞 노드 이름들
    Lobby_Gear_RequireNone,
    Lobby_Gear_CostFormat,          // {0} 골드
    Lobby_Gear_Open,
    Lobby_Gear_Opened,
    Lobby_Gear_Locked,
    Lobby_Gear_NotEnoughGold,
    Lobby_Gear_EquipComingSoon,
    Lobby_Gear_Stat_Attack,         // {0} %
    Lobby_Gear_Stat_MaxHp,          // {0} %
    Lobby_Gear_Stat_Dodge,          // {0} %
    Lobby_Gear_Stat_Defense,        // {0} %
    Lobby_Gear_Stat_AttackSpeed,    // {0} %
    Lobby_Gear_Stat_MoveSpeed,      // {0} %
    Lobby_Gear_Stat_PartDamage,     // {0} %
    Lobby_Gear_Stat_DashDistance,   // {0} %
    Lobby_Gear_Stat_BreakResist,    // {0} 경직 수치

    // battle
    Battle_Exit,
    Battle_ExitConfirm,
    Battle_Enrage,
    Battle_RoarEnragedFormat,   // {0} 보스 이름
    Battle_LootBothFormat,      // {0} 버섯 {1} 신선 {2} 덜 신선
    Battle_LootFreshFormat,     // {0} 버섯 {1} 신선
    Battle_LootStaleFormat,     // {0} 버섯 {1} 덜 신선

    // clock (UINightClock, 전투·시장 공용)
    Clock_MarketCloseFormat,    // {0} 시간 {1} 분
    Clock_ActionTimeFormat,     // {0} 행동당 분

    // market (PopupMarket·PopupMarketCounter·PopupMarket_MerchantRow)
    Market_MerchantCaption,
    Market_Close,
    Market_WaitFormat,               // {0} 분
    Market_PickCaption,
    Market_StockCaption,
    Market_Trade,
    Market_SoldFormat,               // {0} 판 골드
    Market_TotalFormat,              // {0} 합계 골드 {1} 고른 묶음 {2} 묶음 상한
    Market_NoMerchant,
    Market_MerchantInfoFormat,       // {0} 신뢰도 {1} 오른 신뢰도 {2} 시세 {3} 선호 희귀도 범위 {4} 선호 배율 {5} 그 밖 배율
    Market_PreferOnlyFormat,         // {0} 희귀도
    Market_PreferAboveFormat,        // {0} 희귀도
    Market_SwingNormal,
    Market_NotBuying,
    Market_PriceFormat,              // {0} 한 개 값
    Market_SellAll,
    Market_TradeGoldFormat,          // {0} 받은 골드
    Market_WaitDone,
    Market_Result_Closed,
    Market_Result_NoMerchant,
    Market_Result_OverLimitFormat,   // {0} 묶음 상한
    Market_Result_NotEnough,
    Market_Result_NotBuying,
    Market_IntroFormat,              // {0} 시간(꾸밈 포함)
    Market_IntroContinue,
    Market_HourFormat,               // {0} 시
    Market_HourMinuteFormat,         // {0} 시 {1} 분
    Market_MinuteFormat,             // {0} 분
    Market_Counter_CountFormat,      // {0} 팔 개수 {1} 가진 개수
    Market_Counter_PriceFormat,      // {0} 한 개 값 {1} 합계
    Market_Counter_NotBuying,
    Market_Counter_All,
    Market_Row_TrustFormat,          // {0} 신뢰도

    // death
    Death_Title,
    Death_Desc,
    Death_ReviveFormat,         // {0} 골드
    Death_Home,

    // result
    Result_TitleExited,
    Result_TitleTimedOut,
    Result_TitleEmpty,
    Result_SummaryFormat,       // {0} 시간 {1} 신뢰도 줄(없으면 빈 문자열)
    Result_TrustPrefix,
    Result_TrustEntryFormat,    // {0} 상인 {1} 오른 신뢰도
    Result_LootHeader,
    Result_LootEmpty,
    Result_Retry,
    Result_Lobby,
}

// ==================================================

// UILabel·StringDef 표에서 키(string) 칸을 EStringKey 드롭다운으로 보여 준다(드로어는 Editor/UI/StringKeyDrawer)
public class StringKeyAttribute : PropertyAttribute { }

// ==================================================

// 문구 표. 키(EStringKey 이름) → 문구. Resources/Data/UI/String.asset 하나만 두고 S_Get·S_Format·UILabel이 읽는다.
// 새 키는 enum에 한 줄 추가하고 "UI 프리팹 표 갱신"을 돌리면 빈 값으로 들어간다(빈 값·enum에 없는 키는 그 메뉴가 경고한다).
// 지금은 한국어 한 열. 못 찾으면 키 이름을 그대로 보여 주고 경고한다

public class StringDef : DataDef
{
    public const string StringID = "String";

    [Serializable]
    public class Entry
    {
        [field: SerializeField, StringKey] public string Key { get; private set; }
        [field: SerializeField, TextArea] public string Value { get; private set; }

        public Entry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    [SerializeField] private List<Entry> m_ListEntry = new();
    private readonly Dictionary<string, string> m_DicValue = new(); // 런타임 조회용. 처음 찾을 때 채운다

    private static readonly Dictionary<EStringKey, string> s_DicKeyName = new(); // enum → 이름. ToString 할당을 한 번만
    private static readonly HashSet<string> s_SetWarned = new(); // 같은 경고를 반복하지 않는다
    private static StringDef s_EditorDef; // EditorGet이 찾은 표. 지워졌으면 다시 찾는다

    // ------------------------------------------------

    public static string S_GetName(EStringKey key)
    {
        if (s_DicKeyName.TryGetValue(key, out string name) == false)
        {
            name = key.ToString();
            s_DicKeyName[key] = name;
        }

        return name;
    }

    public static string S_Get(EStringKey key) => S_Get(S_GetName(key));

    // 표가 없거나 키가 없으면 키 이름(경고 한 번)
    public static string S_Get(string key)
    {
        if (key.E_IsNullOrEmpty())
            return string.Empty;

        if (GameDatabase.S_TryGet(StringID, out StringDef def) && def.TryGet(key, out string value))
            return value;

        if (s_SetWarned.Add(key))
            CLogger.Warning("StringDef", $"{key} 문구가 없습니다. MushTaker/UI/UI 프리팹 표 갱신을 실행하고 String.asset에 값을 넣으세요.");

        return key;
    }

    // 문구의 {n} 자리가 args와 맞지 않으면 예외 대신 경고하고 문구를 그대로 보여 준다
    public static string S_Format(EStringKey key, params object[] args)
    {
        string format = S_Get(key);
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            CLogger.Warning("StringDef", $"{S_GetName(key)} 문구의 {{n}} 자리가 인자 {args.Length}개와 맞지 않습니다: {format}");
            return format;
        }
    }

    public bool TryGet(string key, out string value)
    {
        if (m_DicValue.Count == 0)
        {
            foreach (Entry entry in m_ListEntry)
            {
                if (entry != null && entry.Key.E_IsNullOrEmpty() == false)
                    m_DicValue[entry.Key] = entry.Value;
            }
        }

        return m_DicValue.TryGetValue(key, out value) && value.E_IsNullOrEmpty() == false;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        m_DicValue.Clear();
    }

    public IReadOnlyList<Entry> EditorList => m_ListEntry;

    // UISetup 메뉴 전용
    public void EditorSetList(List<Entry> listEntry)
    {
        m_ListEntry = listEntry;
        m_DicValue.Clear();
    }

    // 에디터 미리보기용(UILabel·UI 생성기). 런타임 DB 없이 에셋을 직접 찾는다(한 번 찾으면 기억). 없으면 키 이름
    public static string EditorGet(string key)
    {
        if (key.E_IsNullOrEmpty())
            return string.Empty;

        if (s_EditorDef == null)
        {
            string guid = UnityEditor.AssetDatabase.FindAssets("t:StringDef").E_GetOrDefault(0);
            if (guid != null)
                s_EditorDef = UnityEditor.AssetDatabase.LoadAssetAtPath<StringDef>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        }

        return s_EditorDef != null && s_EditorDef.TryGet(key, out string value) ? value : key;
    }
#endif
}
