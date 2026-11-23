using UnityEngine;

// ==================================================

// 버섯 신선도. 살아 있을 때 부위를 깨서 얻으면 상급, 쓰러진 뒤 얻으면 하급
public enum EFreshness
{
    High,
    Low,
}

// 버섯 희귀도. 떨어질 때 지역(MapDef) 가중치로 정해진다
public enum ERarity
{
    Common,
    Rare,
    Epic,
}

// 땅에 떨어지는 픽업이 있는 아이템(버섯·재화). 픽업 프리팹은 이 경로로 찾는다(Def는 프리팹을 참조하지 않는다)
public interface IPickupItem
{
    string PickupPath { get; }
}

// ==================================================

// 바닥에 떨어진 버섯(골드 동전도 같은 방식, ItemID만 재화). 튀어 올랐다 떨어진 뒤, 플레이어가 가까이 오면 빨려 들어가 주워진다.
// BattleService.SpawnPickup으로 꺼내고(움직임은 BattleService.Tick, 판이 끝나면 남은 것을 돌려받는다), 주우면 DespawnPickup으로 돌려준다. 여러 개를 흩뿌릴 때는 S_Drop
public class MushroomPickup : PoolableMono
{
    private const float Gravity = -20f;
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)
    private const float SpinSpeed = 120f;        // 바닥에서 도는 속도(초당 각도)

    [SerializeField] private string m_ItemID = string.Empty;       // 인벤토리에 넣을 ID(MushroomDef ID)
    [SerializeField] private int m_Amount = 1;                     // 한 번에 주워지는 개수

    private Vector3 m_Velocity;
    private bool m_IsLanded;
    private bool m_IsMagnet;
    private float m_Age;
    private float m_GroundY;
    private EffectController m_Beam;    // 희귀도 빛기둥(Rare·Epic만). 땅에 닿으면 켜고 주울 때 끈다

    public string ItemID => m_ItemID;
    public EFreshness Freshness { get; private set; }
    public ERarity Rarity { get; private set; }

    // ------------------------------------------------

    // BattleService.Tick이 매 프레임 부른다(픽업마다 Update를 두지 않는다)
    public void Tick(float deltaTime)
    {
        m_Age += deltaTime;

        if (m_IsMagnet)
        {
            UpdateMagnet(deltaTime);
            return;
        }

        if (m_IsLanded == false)
            UpdateFall(deltaTime);
        else
            UpdateIdle(deltaTime);

        if (m_Age >= Define.Drop.PickupActivateDelay && IsPlayerNear())
        {
            m_IsMagnet = true;
            HideBeam();
        }
    }

    // origin 위로 count개를 흩뿌린다(몬스터 드랍·골드 동전). 전투 밖이면 떨구지 않는다
    public static void S_Drop(MushroomPickup prefab, Vector3 origin, int count, EFreshness freshness, ERarity rarity)
    {
        BattleService battleS = FlowManager.Instance.Battle?.BattleS;
        if (battleS == null)
            return;

        Vector3 position = origin + Vector3.up * Define.Drop.MonsterDrop.Height;
        for (int i = 0; i < count; i++)
        {
            MushroomPickup pickup = battleS.SpawnPickup(prefab, position);
            if (pickup == null)
                return;

            Vector2 spread = Random.insideUnitCircle * Define.Drop.MonsterDrop.Spread;
            pickup.Launch(new Vector3(spread.x, Define.Drop.MonsterDrop.UpSpeed, spread.y), freshness, rarity);
        }
    }

    public void Launch(Vector3 velocity, EFreshness freshness, ERarity rarity)
    {
        Freshness = freshness;
        Rarity = rarity;
        m_Velocity = velocity;
        m_IsLanded = false;
        m_IsMagnet = false;
        m_Age = 0f;
        
        HideBeam();
    }

    // virtual ---

    public override void OnPoolDespawn()
    {
        HideBeam();
    }

    // ==================================================

    private void UpdateFall(float deltaTime)
    {
        m_Velocity.y += Gravity * deltaTime;
        Vector3 next = transform.position + m_Velocity * deltaTime;

        // 땅에 닿으면 멈춘다(콜라이더가 있는 지형만)
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        float rayLength = 0.5f + Mathf.Max(0f, -m_Velocity.y * deltaTime) + 0.05f;

        // [mj] todo) 콜라이더에서 대상 찾기. 레이어 검사로 바꿀지 검토
        if (m_Velocity.y < 0f &&
            Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, rayLength, ~0, QueryTriggerInteraction.Ignore) &&
            hitInfo.collider.GetComponentInParent<CharacterController>() == null)
        {
            next.y = hitInfo.point.y;
            m_GroundY = next.y;
            m_IsLanded = true;
            ShowBeam(next);
        }

        transform.position = next;
    }

    private void UpdateIdle(float deltaTime)
    {
        Vector3 position = transform.position;
        position.y = m_GroundY + (Mathf.Sin(m_Age * 3f) + 1f) * 0.08f;
        
        transform.position = position;
        transform.Rotate(0f, SpinSpeed * deltaTime, 0f, Space.World);
    }

    private void UpdateMagnet(float deltaTime)
    {
        // 빨려 드는 중에 쓰러지면(떨군 버섯이 다시 주워지지 않게) 그 자리에서 다시 떨어진다
        UnitController player = FlowManager.Instance.Battle?.UnitS.Player;
        if (player == null || player.IsValid() == false)
        {
            m_IsMagnet = false;
            m_IsLanded = false;
            m_Velocity = Vector3.zero;
            return;
        }

        Vector3 target = player.transform.position + Vector3.up * 0.8f;
        transform.position = Vector3.MoveTowards(transform.position, target, Define.Drop.PickupMagnetSpeed * deltaTime);
        
        if (Vector3.Distance(transform.position, target) > Define.Drop.PickupCollectDistance)
            return;

        Collect();
    }

    private bool IsPlayerNear()
    {
        UnitController player = FlowManager.Instance.Battle?.UnitS.Player;
        if (player == null || player.IsValid() == false)
            return false;

        return Vector3.Distance(player.transform.position, transform.position) <= Define.Drop.PickupRadius;
    }

    // 희귀도 빛기둥. 색은 희귀도별 프리팹에 박혀 있다
    private void ShowBeam(Vector3 groundPosition)
    {
        ECommonEffectType type;
        switch (Rarity)
        {
            case ERarity.Rare: type = ECommonEffectType.RarityBeam_Rare; break;
            case ERarity.Epic: type = ECommonEffectType.RarityBeam_Epic; break;
            default: return;
        }

        m_Beam = FlowManager.Instance.EffectS?.Show(type, groundPosition, Quaternion.identity);
    }

    private void HideBeam()
    {
        FlowManager.Instance.EffectS?.Hide(m_Beam);
        m_Beam = null;
    }

    private void Collect()
    {
        SEventManager.Publish(new LootPicked(m_ItemID, m_Amount, Freshness, Rarity));
        FlowManager flowM = FlowManager.Instance;
        flowM.EffectS?.Play(ECommonEffectType.Pickup, transform.position, Quaternion.identity);

        BattleService battleS = flowM.Battle?.BattleS;
        if (battleS != null)
            battleS.DespawnPickup(this);
        else
            ResourceManager.S_Despawn(this);
    }
}
