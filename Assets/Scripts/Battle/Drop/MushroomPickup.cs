using UnityEngine;

// 바닥에 떨어진 버섯. 튀어 올랐다 떨어진 뒤, 플레이어가 가까이 오면 빨려 들어가 주워진다
public class MushroomPickup : MonoBehaviour
{
    [SerializeField, Tooltip("인벤토리 ID (MushroomDef ID)")] private string m_ItemID = "Mushroom_Songi";
    [SerializeField] private int m_Amount = 1;
    [SerializeField] private float m_PickupRadius = 1.8f;
    [SerializeField] private float m_MagnetSpeed = 12f;
    [SerializeField, Tooltip("떨어진 뒤 주울 수 있기까지")] private float m_ActivateDelay = 0.5f;
    [SerializeField] private float m_SpinSpeed = 120f;

    private const float Gravity = -20f;
    private const float CollectDistance = 0.5f;

    private Vector3 m_Velocity;
    private bool m_IsLanded;
    private bool m_IsMagnet;
    private float m_Age;
    private float m_GroundY;

    public string ItemID => m_ItemID;

    // ==================================================

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

        if (m_Age >= m_ActivateDelay && IsPlayerNear())
            m_IsMagnet = true;
    }

    public void Launch(Vector3 velocity)
    {
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
        if (m_Velocity.y < 0f && Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, rayLength, ~0, QueryTriggerInteraction.Ignore)
            && hitInfo.collider.GetComponentInParent<CharacterController>() == null)
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
        transform.Rotate(0f, m_SpinSpeed * deltaTime, 0f, Space.World);
    }

    private void UpdateMagnet(float deltaTime)
    {
        PlayerController player = PlayerController.Current;
        if (player == null)
        {
            m_IsMagnet = false;
            return;
        }

        Vector3 target = player.transform.position + Vector3.up * 0.8f;
        transform.position = Vector3.MoveTowards(transform.position, target, m_MagnetSpeed * deltaTime);
        if (Vector3.Distance(transform.position, target) > CollectDistance)
            return;

        Collect();
    }

    private bool IsPlayerNear()
    {
        PlayerController player = PlayerController.Current;
        if (player == null || player.IsDead)
            return false;

        return Vector3.Distance(player.transform.position, transform.position) <= m_PickupRadius;
    }

    private void Collect()
    {
        EventBus.Publish(new LootPicked(m_ItemID, m_Amount));
        Destroy(gameObject);
    }
}
