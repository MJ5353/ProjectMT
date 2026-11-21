using System.Collections.Generic;

// ==================================================

// 팝업 종류. 팝업 ID로 쓴다(PopupService.Open<T>(EPopupType)). 새 팝업은 여기와 PopupTable.s_DicPath에 한 줄씩 추가한다
public enum EPopupType
{
    None,
    PopupDeath,
    PopupResult,
    PopupMarket,
    PopupMarket_Count,
}

// ==================================================

// 팝업 ID → 프리팹 경로 표. 경로는 Resources 기준·확장자 없음. 팝업이 아닌 UI(칸·줄·HUD) 경로는 CommonPath에 둔다

public static class PopupTable
{
    private const string MissingHint = "MushTaker/UI/모든 UI 다시 만들기를 실행하세요.";

    // [mj] todo) 이게 왜 여기에...???
    private static readonly Dictionary<EPopupType, string> s_DicPath = new()
    {
        { EPopupType.PopupDeath, CommonPath.PopupUIDir + "PopupDeath" },
        { EPopupType.PopupResult, CommonPath.PopupUIDir + "PopupResult" },
        { EPopupType.PopupMarket, CommonPath.PopupUIDir + "PopupMarket" },
        { EPopupType.PopupMarket_Count, CommonPath.PopupUIDir + "PopupMarket_Count" },
    };

    // ------------------------------------------------

    // 표에 없으면 null(에러 로그)
    public static string GetPath(EPopupType type)
    {
        if (s_DicPath.TryGetValue(type, out string path))
            return path;

        CLogger.Error("PopupTable", $"{type} 경로가 PopupTable에 없습니다.");
        return null;
    }

    // 원본 프리팹(ResourceManager가 캐시). 경로가 없거나 T가 맞지 않으면 null
    public static T Load<T>(EPopupType type) where T : Popup
    {
        string path = GetPath(type);
        if (path == null)
            return null;

        return ResourceManager.Instance?.Load<T>(path, MissingHint);
    }
}
