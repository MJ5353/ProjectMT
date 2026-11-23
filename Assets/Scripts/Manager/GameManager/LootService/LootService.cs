using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// ==================================================

// 버섯·재화가 땅에 떨어져 주머니를 거쳐 보상이 되기까지. 땅 위 픽업(꺼내기·움직이기·돌려받기), 주운 버섯(신선도별)·주운 재화, 쓰러질 때 떨구기, 끝난 이유별 보상 정산을 맡는다.
// 언제 할지는 BattleFlow 상태가 정한다(Down: DropLoot, Market: SetMarketReward, Result: Settle). 저장은 BattleService.Finish가 한다
public sealed class LootService : ServiceBase
{
    private readonly Dictionary<LootKey, int> m_DicLoot = new(); // 주운 버섯. 신선도별
    private readonly Dictionary<string, int> m_DicReward = new(); // 끝날 때 실제로 인벤토리에 넣은 것(결과 화면용)
    private readonly Dictionary<string, int> m_DicPickedCurrency = new(); // 전투 중 주워 바로 인벤토리에 넣은 재화(골드 동전). 결과 화면에 더해 보여 준다
    private readonly List<MushroomPickup> m_ListPickup = new(); // 땅에 떨어져 있는 픽업. 판이 끝나면(Clear) 남은 것을 돌려받는다
    private readonly MapDef m_Map;

    private bool m_IsMarketRewarded; // 시장이 보상을 이미 인벤토리에 넣었다(SetMarketReward)

    private static DefineDef Define => GameManager.Instance.Define;
    public IReadOnlyDictionary<LootKey, int> DicLoot => m_DicLoot;
    public IReadOnlyDictionary<string, int> DicReward => m_DicReward;

    // ------------------------------------------------

    public LootService(MapDef mapDef)
    {
        m_Map = mapDef;
    }

    // virtual ---

    public override void Init()
    {
        SEventManager.Subscribe<LootPicked>(OnLootPicked);
    }

    // 땅에 떨어진 픽업을 움직인다(낙하·제자리·빨려 듦). 시장에서 시간이 멈추면 같이 멈춘다
    public override void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        // 주우면 목록에서 빠지므로 뒤에서부터 돈다
        for (int i = m_ListPickup.Count - 1; i >= 0; i--)
        {
            m_ListPickup[i].Tick(deltaTime);
        }
    }

    public override void Clear()
    {
        SEventManager.Unsubscribe<LootPicked>(OnLootPicked);

        // 다시 하기에 떨어진 버섯이 남지 않게 돌려받는다(빛기둥은 픽업이 돌아갈 때 끈다). 씬과 함께 지워진 것은 건너뛴다
        foreach (MushroomPickup pickup in m_ListPickup)
        {
            ResourceManager.S_Despawn(pickup);
        }

        m_ListPickup.Clear();
        m_DicLoot.Clear();
        m_DicReward.Clear();
        m_DicPickedCurrency.Clear();

        m_IsMarketRewarded = false;
    }

    // 떨어지는 버섯 하나의 희귀도. 지금 지역(MapDef)의 가중치로 굴린다. 전투 밖이면 Common
    public static ERarity RollRarity()
    {
        MapDef map = FlowManager.Instance.Battle?.LootS.m_Map;
        return map.E_Alive()?.RollRarity() ?? ERarity.Common;
    }

    // pick up ---
    
    // 땅에 떨어지는 픽업(버섯·골드 동전)을 꺼낸다. 움직임은 Tick이 돌리고, 판이 끝나면 남은 것을 돌려받는다
    public MushroomPickup SpawnPickup(MushroomPickup prefab, Vector3 position)
    {
        MushroomPickup pickup = ResourceManager.Instance.Spawn(prefab, position, Quaternion.identity);
        if (pickup != null)
            m_ListPickup.Add(pickup);

        return pickup;
    }

    // 주운 픽업을 돌려준다
    public void DespawnPickup(MushroomPickup pickup)
    {
        m_ListPickup.Remove(pickup);
        ResourceManager.S_Despawn(pickup);
    }

    // 재화(골드 동전)는 바로 인벤토리에 넣고(귀가해도 남는다), 버섯은 주운 목록에 넣는다. 그다음 플레이어 패시브(주웠을 때)
    private void OnLootPicked(LootPicked evt)
    {
        if (GameDatabase.S_TryGet(evt.ItemID, out CurrencyDef _))
        {
            PlayerDataManager.Instance.Inventory.Add(evt.ItemID, evt.Amount);

            m_DicPickedCurrency.TryGetValue(evt.ItemID, out int picked);
            m_DicPickedCurrency[evt.ItemID] = picked + evt.Amount;
        }
        else
        {
            LootKey key = new(evt.ItemID, evt.Freshness, evt.Rarity);
            m_DicLoot.TryGetValue(key, out int count);
            m_DicLoot[key] = count + evt.Amount;

            SEventManager.Publish(new LootChanged());
        }

        UnitController player = FlowManager.Instance.Battle.UnitS.Player;
        if (player != null)
            player.PassiveF.OnPickup(evt);
    }

    // result ---
    
    // 주운 버섯을 진짜 픽업으로 흩뿌린다. 부활하면 다시 주울 수 있고, 귀가하면 그대로 잃는다. Down 상태가 부른다
    public void DropLoot(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * Define.Drop.DownDrop.Height;
        foreach (var pair in m_DicLoot)
        {
            if (GameDatabase.S_TryGet(pair.Key.ItemID, out MushroomDef def) == false)
                continue;

            MushroomPickup prefab = ResourceManager.Instance.Load<MushroomPickup>(def.PickupPath);
            if (prefab == null)
                continue;

            // shortcut: 픽업 하나를 1개로 본다(MushroomPickup.m_Amount 기본값), 묶음 픽업이 생기면 개수를 나눈다
            for (int i = 0; i < pair.Value; i++)
            {
                MushroomPickup pickup = SpawnPickup(prefab, origin);
                Vector2 spread = Random.insideUnitCircle * Define.Drop.DownDrop.Spread;
                pickup.Launch(new Vector3(spread.x, Define.Drop.DownDrop.UpSpeed, spread.y), pair.Key.Freshness, pair.Key.Rarity);
            }
        }

        m_DicLoot.Clear();
        SEventManager.Publish(new LootChanged());
    }

    // 시장이 닫혔다(시장 닫기 또는 밤이 다 감). 골드·썩은 버섯·신뢰도는 HuntData가 이미 넣었다. 결과 화면용으로만 남긴다
    public void SetMarketReward(MarketClosed evt)
    {
        m_DicReward.Clear();

        if (evt.Gold > 0)
            m_DicReward[CurrencyDef.GoldID] = evt.Gold;

        if (evt.RottenCount > 0)
            m_DicReward[ItemDef.RottenID] = evt.RottenCount;

        m_IsMarketRewarded = true;
    }

    // 끝난 이유대로 보상을 인벤토리에 넣고 결과 화면용 목록(DicReward)을 채운다. BattleFlow Result 상태가 한 번 부른다(저장은 그다음 BattleService.Finish)
    public void Settle(EHuntResult result)
    {
        if (m_IsMarketRewarded == false)
        {
            FillReward(result);

            InventoryData inventory = PlayerDataManager.Instance.Inventory;
            foreach (var pair in m_DicReward)
            {
                inventory.Add(pair.Key, pair.Value);
            }
        }

        // 전투 중 주운 재화는 이미 인벤토리에 있다. 결과 화면에만 더한다(귀가·쓰러짐에도 남는다)
        foreach (var pair in m_DicPickedCurrency)
        {
            m_DicReward.TryGetValue(pair.Key, out int count);
            m_DicReward[pair.Key] = count + pair.Value;
        }
    }

    // 시장을 거치지 않고 끝날 때. 밤이 끝나면 주운 개수만큼 썩은 버섯, 귀가는 빈손(토벌 종료는 늘 시장을 거친다)
    private void FillReward(EHuntResult result)
    {
        m_DicReward.Clear();
        if (result != EHuntResult.TimedOut)
            return;

        int total = 0;
        foreach (var pair in m_DicLoot)
        {
            total += pair.Value;
        }

        if (total > 0)
            m_DicReward[ItemDef.RottenID] = total;
    }
}
