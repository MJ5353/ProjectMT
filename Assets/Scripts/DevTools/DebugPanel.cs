#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

// 개발용 치트 패널(IMGUI). 오른쪽 위 DBG 버튼으로 연다. 릴리스 빌드에는 들어가지 않는다.
// 씬 이동, 몬스터 소환, 시장(버섯 줍기·시간·상인·신뢰도), 정비(초기화·골드), 데이터베이스 아이템 지급, 세이브 저장·삭제를 한 곳에서 한다
public class DebugPanel : MonoBehaviour
{
    private const float RefHeight = 960f;
    private const string DefaultMapID = "Map_001_Field"; // Battle 씬 단독 실행 때 쓰는 지역
    private const float SpawnGap = 2f; // 플레이어 캡슐과 소환 몬스터 캡슐 사이 거리(m)
    private const int DebugLootCount = 5; // 시장 시험용으로 한 번에 줍는 버섯 수
    private const int DebugTrustStep = 10;
    private static readonly int[] s_DebugGoldSteps = { 1000, 10000 }; // 정비 시험용 골드

    private static readonly ESceneType[] s_MoveScenes = { ESceneType.Title, ESceneType.Lobby, ESceneType.Battle };

    private bool m_IsOpen = false;
    private Vector2 m_ScrollPos = Vector2.zero;

    // ------------------------------------------------

    private void OnGUI()
    {
        GameManager gameM = GameManager.Instance;

        // 해상도와 상관없이 비슷한 크기로 보이게 높이 기준으로 확대한다
        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        if (GUI.Button(new Rect(width - 70, 10, 60, 30), m_IsOpen ? "닫기" : "DBG"))
            m_IsOpen = m_IsOpen == false;

        if (m_IsOpen == false)
            return;

        GUILayout.BeginArea(new Rect(width - 290, 50, 280, RefHeight - 60), GUI.skin.box);
        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);

        DrawFlow(gameM);
        GUILayout.Space(8);

        DrawSpawn(gameM.Database);
        GUILayout.Space(8);
        
        DrawMarket(gameM);
        GUILayout.Space(8);

        DrawGear();
        GUILayout.Space(8);
        
        DrawItems(gameM);
        GUILayout.Space(8);
        
        DrawSave();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // 지금 Flow 이름과 씬 이동 버튼. 지금 씬 버튼은 다시 불러오기
    private void DrawFlow(GameManager gameM)
    {
        FlowBase flow = FlowManager.Instance.Current;
        GUILayout.Label($"흐름: {(flow != null ? flow.GetType().Name : "없음")}");
        GUILayout.BeginHorizontal();

        foreach (ESceneType type in s_MoveScenes)
        {
            bool isCurrent = IsCurrentScene(flow, type);
            GUI.enabled = isCurrent ? gameM.IsTransitioning == false : gameM.CanChangeScene(type);
            
            if (GUILayout.Button(isCurrent ? $"{type}(다시)" : type.ToString()) == false)
                continue;

            if (isCurrent)
                gameM.ReloadScene();
            else
                gameM.ChangeScene(type);
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private static bool IsCurrentScene(FlowBase flow, ESceneType type)
    {
        return type switch
        {
            ESceneType.Title => flow is TitleFlow,
            ESceneType.Lobby => flow is LobbyFlow,
            ESceneType.Battle => flow is BattleFlow,
            _ => false,
        };
    }

    // 전투 중일 때만 몬스터 목록을 보여 주고, 누르면 플레이어 앞에 소환한다
    private void DrawSpawn(GameDatabase database)
    {
        UnitService units = FlowManager.Instance.Battle?.UnitS;
        if (units == null || units.Player == null || database == null)
            return;

        GUILayout.Label("몬스터 소환(플레이어 앞)");
        foreach (MonsterDef def in database.GetAll<MonsterDef>())
        {
            if (def == null)
                continue;

            if (GUILayout.Button(def.ID))
                SpawnInFront(units, def);
        }
    }

    private static void SpawnInFront(UnitService units, MonsterDef def)
    {
        Transform player = units.Player.transform;
        float distance = SpawnGap + units.Player.HurtBody.radius;
        
        UnitController prefab = ResourceManager.Instance.Load<UnitController>(def.PrefabPath);
        if (prefab != null && prefab.TryGetComponent(out CharacterController controller))
            distance += controller.radius;

        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        Vector3 position = player.position + forward * distance + Vector3.up * BattleService.SpawnLift;
        
        units.Spawn(def, position, Quaternion.LookRotation(-forward, Vector3.up));
    }

    // 시장 시험용. 전투 중이면 버섯 줍기, 시장 중이면 남은 밤·상인, 언제나 신뢰도
    private void DrawMarket(GameManager gameM)
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        if (gameM.Database == null)
            return;

        GUILayout.Label("시장");
        BattleFlow battle = FlowManager.Instance.Battle;
        HuntData hunt = playerDataM.Hunt;

        if (battle != null && battle.State == EBattleFlowState.Play && GUILayout.Button($"버섯 {DebugLootCount}개 줍기(신선도·희귀도 랜덤)"))
            PickRandomLoot(gameM.Database);

        if (hunt.IsMarketOpen)
        {
            float actionTime = gameM.Define.MarketActionTime;
            
            GUILayout.Label($"남은 밤 {hunt.RemainTime:0.0}초");
            GUILayout.BeginHorizontal();

            if (GUILayout.Button($"+{gameM.Define.MarketActionMinutes}분"))
                hunt.DebugAddTime(actionTime);

            if (GUILayout.Button($"-{gameM.Define.MarketActionMinutes}분"))
                hunt.DebugAddTime(-actionTime);

            if (GUILayout.Button("상인 다시"))
                hunt.DebugRerollMerchants();

            GUILayout.EndHorizontal();
        }

        MarketData market = playerDataM.Market;
        foreach (MerchantDef merchant in gameM.Database.GetAll<MerchantDef>())
        {
            if (merchant == null)
                continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{merchant.DisplayName} 신뢰 {market.GetTrust(merchant.ID)}", GUILayout.Width(170));

            if (GUILayout.Button($"+{DebugTrustStep}"))
                market.AddTrust(merchant.ID, DebugTrustStep);

            if (GUILayout.Button($"-{DebugTrustStep}"))
                market.AddTrust(merchant.ID, -DebugTrustStep);

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("신뢰도 모두 0"))
            market.DebugResetTrust();
    }

    // 정비 시험용. 초기화는 연 노드만 지운다(낸 골드는 돌려주지 않는다)
    private void DrawGear()
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        GearData gear = playerDataM.Gear;

        GUILayout.Label($"정비 {gear.OpenedCount}/{gear.TotalCount}, 골드 {playerDataM.Inventory.GetCount(CurrencyDef.GoldID)}");
        GUILayout.BeginHorizontal();

        foreach (int gold in s_DebugGoldSteps)
        {
            if (GUILayout.Button($"골드 +{gold}"))
                playerDataM.Inventory.Add(CurrencyDef.GoldID, gold);
        }

        if (GUILayout.Button("정비 초기화"))
        {
            gear.DebugReset();
            playerDataM.RunSave();
        }

        GUILayout.EndHorizontal();
    }

    // 실제로 주운 것처럼 LootPicked를 보낸다(희귀도는 지금 지역 가중치)
    private static void PickRandomLoot(GameDatabase database)
    {
        List<MushroomDef> listMushroom = new(database.GetAll<MushroomDef>());
        if (listMushroom.Count == 0)
            return;

        for (int i = 0; i < DebugLootCount; ++i)
        {
            MushroomDef def = listMushroom[Random.Range(0, listMushroom.Count)];
            EFreshness freshness = CRandom.Chance(0.5f) ? EFreshness.High : EFreshness.Low;
            
            SEventManager.Publish(new LootPicked(def.ID, 1, freshness, BattleService.RollRarity()));
        }
    }

    public static MapDef GetDefaultMap(GameDatabase database)
    {
        if (database == null || database.TryGet(DefaultMapID, out MapDef map) == false)
            return null;

        CLogger.Warning("DebugPanel", $"고른 지역이 없어 개발용 기본 지역 {DefaultMapID}을 씁니다.");
        return map;
    }

    private void DrawItems(GameManager gameM)
    {
        GUILayout.Label("아이템 지급");
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        
        if (gameM.Database == null)
            return;

        InventoryData inventory = playerDataM.Inventory;
        foreach (DataDef def in gameM.Database.ListDef)
        {
            if (def == null)
                continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{def.DisplayName} ({inventory.GetCount(def.ID)})", GUILayout.Width(170));

            if (GUILayout.Button("+1"))
                inventory.Add(def.ID, 1);

            if (GUILayout.Button("+10"))
                inventory.Add(def.ID, 10);

            GUILayout.EndHorizontal();
        }
    }

    private void DrawSave()
    {
        PlayerDataManager playerDataM = PlayerDataManager.Instance;
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("저장"))
            playerDataM.RunSave();

        if (GUILayout.Button("세이브 삭제"))
            playerDataM.ResetSave();

        GUILayout.EndHorizontal();
    }
}
#endif
