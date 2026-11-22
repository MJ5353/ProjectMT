using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// UI 데이터 표(StringDef·RarityDef)를 맞춘다. UISetup의 "UI 프리팹 표 갱신"이 부른다.
// 이미 있는 값은 건드리지 않고, 없는 키·희귀도만 초기값으로 채운다. 값은 에셋에서 고친다

public static class UIDataSetup
{
    // 키를 처음 표에 넣을 때만 쓰는 초기값(한국어). 여기 없는 새 키는 빈 값으로 들어가 경고가 난다
    private static readonly Dictionary<EStringKey, string> s_DicDefaultString = new()
    {
        { EStringKey.Common_GameTitle, "버섯을 줍자!" },
        { EStringKey.Common_Yes, "예" },
        { EStringKey.Common_No, "아니오" },
        { EStringKey.Common_Cancel, "취소" },
        { EStringKey.Common_Confirm, "확인" },

        { EStringKey.Title_PressToStart, "클릭하여 시작하기" },
        { EStringKey.Loading_Text, "불러오는 중..." },

        { EStringKey.Lobby_Tab_Gear, "정비" },
        { EStringKey.Lobby_Tab_Hunt, "토벌" },
        { EStringKey.Lobby_Tab_Breed, "개량" },
        { EStringKey.Lobby_Tab_Dispatch, "파견" },
        { EStringKey.Lobby_ComingSoon, "준비 중" },
        { EStringKey.Lobby_Hunt_MapTitle, "토벌 지도" },
        { EStringKey.Lobby_Hunt_MushroomHeader, "나오는 버섯" },
        { EStringKey.Lobby_Hunt_Enter, "입장" },
        { EStringKey.Lobby_Hunt_NoMap, "지역 없음" },
        { EStringKey.Lobby_Hunt_NoMapDesc, "MushTaker/데이터 편집기에서 MapDef를 준비하세요." },
        { EStringKey.Lobby_Hunt_Cleared, "토벌 완료" },
        { EStringKey.Lobby_Hunt_Unlocked, "토벌 가능" },
        { EStringKey.Lobby_Hunt_LockedFormat, "잠김 · {0}을(를) 먼저 토벌" },

        { EStringKey.Lobby_Gear_TreeTitle, "강화 노드" },
        { EStringKey.Lobby_Gear_EmblemProgressFormat, "강화 {0} / {1}" },
        { EStringKey.Lobby_Gear_EmblemNextFormat, "다음 단계까지 {0}개" },
        { EStringKey.Lobby_Gear_EmblemMax, "최고 단계" },
        { EStringKey.Lobby_Gear_EmblemBonusHeader, "엠블렘 보너스" },
        { EStringKey.Lobby_Gear_NoBonus, "없음" },
        { EStringKey.Lobby_Gear_SelectHint, "노드를 눌러 보세요" },
        { EStringKey.Lobby_Gear_RequireFormat, "조건  {0}" },
        { EStringKey.Lobby_Gear_RequireNone, "조건 없음" },
        { EStringKey.Lobby_Gear_CostFormat, "{0:N0}G" },
        { EStringKey.Lobby_Gear_Open, "강화" },
        { EStringKey.Lobby_Gear_Opened, "강화 완료" },
        { EStringKey.Lobby_Gear_Locked, "잠김" },
        { EStringKey.Lobby_Gear_NotEnoughGold, "골드 부족" },
        { EStringKey.Lobby_Gear_EquipComingSoon, "무기·방어구 장착과 코스튬 변경이 추가될 예정입니다" },

        { EStringKey.Stat_Attack, "공격력 +{0}" },
        { EStringKey.Stat_MaxHp, "최대 체력 +{0}" },
        { EStringKey.Stat_Dodge, "대시 쿨타임 -{0}" },
        { EStringKey.Stat_Defense, "받는 피해 -{0}" },
        { EStringKey.Stat_AttackSpeed, "공격 속도 +{0}" },
        { EStringKey.Stat_MoveSpeed, "이동 속도 +{0}" },
        { EStringKey.Stat_PartDamage, "버섯 부위 피해 +{0}" },
        { EStringKey.Stat_DashDistance, "대시 거리 +{0}" },
        { EStringKey.Stat_BreakResist, "경직 저항 +{0}" },

        { EStringKey.Battle_Exit, "토벌 종료" },
        { EStringKey.Battle_ExitConfirm, "토벌을 종료할까요?" },
        { EStringKey.Battle_Enrage, "분노!" },
        { EStringKey.Battle_RoarEnragedFormat, "{0} 분노!" },
        { EStringKey.Battle_LootBothFormat, "{0} 신선 x{1} / 덜 신선 x{2}" },
        { EStringKey.Battle_LootFreshFormat, "{0} 신선 x{1}" },
        { EStringKey.Battle_LootStaleFormat, "{0} 덜 신선 x{1}" },

        { EStringKey.Clock_MarketCloseFormat, "시장이 닫기까지, {0}h {1:00}m" },
        { EStringKey.Clock_ActionTimeFormat, "행동당 {0}분" },

        { EStringKey.Market_MerchantCaption, "오늘 온 상인" },
        { EStringKey.Market_Close, "시장 닫기" },
        { EStringKey.Market_WaitFormat, "{0}분 기다리기" },
        { EStringKey.Market_PickCaption, "팔 버섯  ·  눌러서 묶음 빼기" },
        { EStringKey.Market_StockCaption, "남은 버섯  ·  칸 아래는 이 상인의 한 개 값" },
        { EStringKey.Market_StockHint, "슬롯을 한 번 클릭하여 전체 넣기, 다시 클릭하여 개수 조절" },
        { EStringKey.Market_Trade, "거래" },
        { EStringKey.Market_SoldFormat, "판 골드 {0:N0}G" },
        { EStringKey.Market_TotalFormat, "합계 {0}G  ({1}/{2}묶음)" },
        { EStringKey.Market_NoMerchant, "오늘은 상인이 없다" },
        { EStringKey.Market_MerchantInfoFormat, "신뢰도  {0}{1}\n오늘 시세  {2}\n{3}  x{4:0.0#}\n그 밖  {5}" },
        { EStringKey.Market_PreferOnlyFormat, "{0}만" },
        { EStringKey.Market_PreferAboveFormat, "{0} 이상" },
        { EStringKey.Market_SwingNormal, "보통" },
        { EStringKey.Market_NotBuying, "안 삼" },
        { EStringKey.Market_PriceFormat, "{0}G" },
        { EStringKey.Market_SellAll, "전체" },
        { EStringKey.Market_TradeGoldFormat, "+{0}G" },
        { EStringKey.Market_WaitDone, "상인들이 값을 다시 매겼다" },
        { EStringKey.Market_Result_Closed, "시장이 닫혔다" },
        { EStringKey.Market_Result_NoMerchant, "상인을 먼저 골라야 한다" },
        { EStringKey.Market_Result_OverLimitFormat, "한 번에 {0}묶음까지 팔 수 있다" },
        { EStringKey.Market_Result_NotEnough, "가진 것보다 많이 골랐다" },
        { EStringKey.Market_Result_NotBuying, "이 상인이 사지 않는 버섯이 섞여 있다" },
        { EStringKey.Market_IntroFormat, "지금 가면... {0}은 거래할 수 있겠어!" },
        { EStringKey.Market_IntroContinue, "눌러서 계속" },
        { EStringKey.Market_HourFormat, "{0}시간" },
        { EStringKey.Market_HourMinuteFormat, "{0}시간 {1}분" },
        { EStringKey.Market_MinuteFormat, "{0}분" },
        { EStringKey.Market_Counter_CountFormat, "판매 {0} / {1}" },
        { EStringKey.Market_Counter_PriceFormat, "한 개 {0}G  ·  합계 {1:N0}G" },
        { EStringKey.Market_Counter_NotBuying, "이 상인은 사지 않는다" },
        { EStringKey.Market_Counter_All, "모두 넣기" },
        { EStringKey.Market_Counter_Clear, "모두 빼기" },
        { EStringKey.Market_Row_TrustFormat, "신뢰 {0}" },

        { EStringKey.Death_Title, "쓰러졌다" },
        { EStringKey.Death_Desc, "귀가하면 주운 버섯을 모두 잃어요" },
        { EStringKey.Death_ReviveFormat, "부활 ({0} 골드)" },
        { EStringKey.Death_Home, "귀가" },

        { EStringKey.Result_TitleExited, "토벌 종료" },
        { EStringKey.Result_TitleTimedOut, "날이 밝았다" },
        { EStringKey.Result_TitleEmpty, "빈손으로 귀가" },
        { EStringKey.Result_SummaryFormat, "시간 {0}{1}" },
        { EStringKey.Result_TrustPrefix, "  ·  신뢰도 " },
        { EStringKey.Result_TrustEntryFormat, "{0} +{1}" },
        { EStringKey.Result_LootHeader, "주운 버섯" },
        { EStringKey.Result_LootEmpty, "주운 버섯이 없어요" },
        { EStringKey.Result_Retry, "다시 하기" },
        { EStringKey.Result_Lobby, "로비로" },
    };

    // ==================================================

    // EStringKey의 새 키를 표 뒤에 붙이고, 빈 값·enum에 없는 키(표·UI 프리팹의 UILabel)를 경고한다
    public static void SyncStringDef()
    {
        StringDef def = LoadOrCreate<StringDef>(EditorPath.StringDefAsset, StringDef.StringID);

        List<StringDef.Entry> listEntry = def.EditorList.Where(e => e != null).ToList();
        HashSet<string> setKey = new(listEntry.Select(e => e.Key));
        HashSet<string> setEnum = new(Enum.GetNames(typeof(EStringKey)).Skip(1)); // None 제외

        int added = 0;
        foreach (EStringKey key in Enum.GetValues(typeof(EStringKey)))
        {
            if (key == EStringKey.None)
                continue;

            string name = StringDef.S_GetName(key);
            if (setKey.Contains(name))
                continue;

            listEntry.Add(new StringDef.Entry(name, s_DicDefaultString.E_GetOrDefault(key, string.Empty)));
            ++added;
        }

        if (added > 0)
        {
            def.EditorSetList(listEntry);
            EditorUtility.SetDirty(def);
            CLogger.Log("UIDataSetup", $"StringDef에 새 키 {added}개를 넣었습니다.");
        }

        foreach (StringDef.Entry entry in listEntry)
        {
            if (setEnum.Contains(entry.Key) == false)
                CLogger.Warning("UIDataSetup", $"StringDef: {entry.Key}는 EStringKey에 없는 키입니다(이름을 바꿨다면 표의 키도 바꾸세요).", def);
            else if (entry.Value.E_IsNullOrEmpty())
                CLogger.Warning("UIDataSetup", $"StringDef: {entry.Key} 값이 비어 있습니다.", def);
        }

        CheckPrefabLabels(setEnum);
    }

    // UI 프리팹 안 UILabel의 키가 enum에 있는지 본다. 씬에 직접 놓은 라벨은 보지 않는다(씬 UI도 프리팹 인스턴스다)
    private static void CheckPrefabLabels(HashSet<string> setEnum)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EditorPath.UIPrefabDir.TrimEnd('/') });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            foreach (UILabel label in prefab.GetComponentsInChildren<UILabel>(true))
            {
                string key = new SerializedObject(label).FindProperty("m_Key").stringValue;
                if (key.E_IsNullOrEmpty() == false && setEnum.Contains(key) == false)
                    CLogger.Warning("UIDataSetup", $"{path}: {label.name}의 UILabel 키 {key}가 EStringKey에 없습니다.", prefab);
            }
        }
    }

    // 없으면 만들고, 빠진 희귀도만 초기값(UI_Slot_* 그림, UIStyle 색, 이름)으로 채운다
    public static void EnsureRarityDef()
    {
        RarityDef def = LoadOrCreate<RarityDef>(EditorPath.RarityDefAsset, RarityDef.RarityID);

        List<RarityDef.Entry> listEntry = def.EditorList.Where(e => e != null).ToList();
        int added = 0;

        foreach (ERarity rarity in Enum.GetValues(typeof(ERarity)))
        {
            if (listEntry.Any(e => e.Rarity == rarity))
                continue;

            listEntry.Add(CreateRarityEntry(rarity));
            ++added;
        }

        if (added == 0)
            return;

        def.EditorSetList(listEntry);
        EditorUtility.SetDirty(def);
        CLogger.Log("UIDataSetup", $"RarityDef에 희귀도 {added}개를 넣었습니다.");
    }

    private static RarityDef.Entry CreateRarityEntry(ERarity rarity)
    {
        return rarity switch
        {
            ERarity.Epic => new RarityDef.Entry(rarity, UIBuilder.LoadSprite("UI_Slot_Epic"), UIStyle.RarityEpic, "에픽"),
            ERarity.Rare => new RarityDef.Entry(rarity, UIBuilder.LoadSprite("UI_Slot_Rare"), UIStyle.RarityRare, "희귀"),
            _ => new RarityDef.Entry(rarity, UIBuilder.LoadSprite("UI_Slot_Common"), UIStyle.RarityCommon, "일반"),
        };
    }

    private static T LoadOrCreate<T>(string assetPath, string dataID) where T : DataDef
    {
        T def = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (def != null)
            return def;

        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);

        def = ScriptableObject.CreateInstance<T>();
        def.EditorSetIdentity(dataID, dataID);
        AssetDatabase.CreateAsset(def, assetPath);
        return def;
    }
}
