using UnityEngine;

// ==================================================

// 버섯 신선도. 살아 있을 때 부위를 깨서 얻으면 상급, 쓰러진 뒤 얻으면 하급
public enum EFreshness
{
    High,
    Low,
}

// ==================================================

// 바닥에 떨어진 버섯. 튀어 올랐다 떨어진 뒤, 플레이어가 가까이 오면 빨려 들어가 주워진다.
// ResourceManager 풀로 꺼내고 돌려준다
public class MushroomPickup : PoolableMono
{
    private const float Gravity = -20f;
    private const float CollectDistance = 0.5f;  // 이 거리까지 빨려 오면 주운 것으로 친다
    private const float PickupRadius = 1.8f;     // 플레이어가 이 거리 안에 오면 빨려 들기 시작
    private const float MagnetSpeed = 12f;
    private const float ActivateDelay = 0.5f;    // 떨어진 뒤 주울 수 있기까지(초)
    private const float SpinSpeed = 120f;        // 바닥에서 도는 속도(초당 각도)

    [SerializeField] private string m_ItemID = string.Empty;       // 인벤토리에 넣을 ID(MushroomDef ID)
    [SerializeField] private int m_Amount = 1;                     // 한 번에 주워지는 개수

    private Vector3 m_Velocity;
    private bool m_IsLanded;
    private bool m_IsMagnet;
    private float m_Age;
    private float m_GroundY;

    public string ItemID => m_ItemID;
    public EFreshness Freshness { get; private set; }

    // ------------------------------------------------

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

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

        if (m_Age >= ActivateDelay && IsPlayerNear())
            m_IsMagnet = true;
    }

    public void Launch(Vector3 velocity, EFreshness freshness)
    {
        Freshness = freshness;
        m_Velocity = velocity;
        m_IsLanded = false;
        m_IsMagnet = false;
        m_Age = 0f;
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
        UnitController player = GameManager.Instance?.UnitS?.Player;
        if (player == null)
        {
            m_IsMagnet = false;
            return;
        }

        Vector3 target = player.transform.position + Vector3.up * 0.8f;
        transform.position = Vector3.MoveTowards(transform.position, target, MagnetSpeed * deltaTime);
        
        if (Vector3.Distance(transform.position, target) > CollectDistance)
            return;

        Collect();
    }

    private bool IsPlayerNear()
    {
        UnitController player = GameManager.Instance?.UnitS?.Player;
        if (player == null || player.IsValid() == false)
            return false;

        return Vector3.Distance(player.transform.position, transform.position) <= PickupRadius;
    }

    private void Collect()
    {
        SEventManager.Publish(new LootPicked(m_ItemID, m_Amount, Freshness));
        EffectService effectS = GameManager.Instance?.EffectS;
        effectS?.Play(effectS.Pickup, transform.position, Quaternion.identity);
        
        ResourceManager.S_Despawn(this);
    }
}
