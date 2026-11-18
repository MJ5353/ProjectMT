using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// ==================================================

// 몸 부위 하나(MonsterDef.PartEntry)와 거기 꽂힌 버섯들. 내구도가 0이 되면 버섯이 같이 깨진 모델로 바뀌고,
// 재생 시간 뒤 작게 다시 돋아 원래 크기로 자란다.
// 버섯 모델은 Set에서 최대 개수만큼 미리 만들어 두고, 스폰마다 Place로 소켓을 다시 골라 옮긴다(풀이 필요 없다)

public class PartFeature : UnitFeature
{
    private const float RegrowTime = 1.5f;         // [mj] todo) 임시값. 다시 돋은 뒤 원래 크기까지 자라는 시간(초)
    private const float RegrowStartScale = 0.3f;   // 다시 돋을 때 처음 크기 배율

    // 꽂힌 버섯 하나. 일반·깨진 모델을 둘 다 소켓에 붙여 두고 켜고 끈다
    public class Mushroom
    {
        public readonly Renderer Normal;
        public readonly Renderer Broken;                    // 없으면 깨질 때 사라진다
        public Transform Socket { get; private set; }       // 이번 스폰에 꽂힌 소켓. null이면 안 꽂힘
        public Vector3 NormalScale { get; private set; }

        public Mushroom(Renderer normal, Renderer broken)
        {
            Normal = normal;
            Broken = broken;
        }

        public void Attach(Transform socket, Quaternion rotation, float scale)
        {
            Socket = socket;
            NormalScale = Vector3.one * scale;
            Set(Normal, socket, rotation, NormalScale);
            Set(Broken, socket, rotation, NormalScale);
        }

        public void Detach()
        {
            Socket = null;
            Normal.gameObject.SetActive(false);
            if (Broken != null)
                Broken.gameObject.SetActive(false);
        }

        private static void Set(Renderer renderer, Transform socket, Quaternion rotation, Vector3 scale)
        {
            if (renderer == null)
                return;

            Transform modelTransform = renderer.transform;
            modelTransform.SetParent(socket, false);
            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = rotation;
            modelTransform.localScale = scale;
        }
    }

    public int Index { get; } // MonsterDef.ListPart 칸 번호 = HitParams.PartIndex
    public MonsterDef.PartEntry Entry { get; }
    public IReadOnlyList<Mushroom> ListMushroom => m_ListMushroom;
    public int PlacedCount => m_PlacedCount; // 이번 스폰에 꽂힌 버섯 수(ListMushroom 앞에서부터)
    public int Durability { get; private set; }
    public bool IsBroken { get; private set; }
    public bool CanBreak => Entry.IsBreakable && IsBroken == false && m_GrowRemain <= 0f && m_PlacedCount > 0;

    private readonly List<Mushroom> m_ListMushroom = new();

    // runtime ---

    private int m_PlacedCount;   // 이번 스폰에 꽂힌 버섯 수(앞에서부터)
    private float m_RegenRemain;
    private float m_GrowRemain;  // 자라는 중에는 깨지지 않는다

    public event Action<PartFeature> Broken;

    // ------------------------------------------------

    public PartFeature(IActor owner, int index, MonsterDef.PartEntry entry) : base(owner)
    {
        Index = index;
        Entry = entry;
    }

    public void AddMushroom(Renderer normal, Renderer broken)
    {
        m_ListMushroom.Add(new Mushroom(normal, broken));
    }

    // 풀에서 다시 꺼낼 때 원상복구. 버섯 위치는 주인이 Place로 다시 고른다
    public override void Reset()
    {
        IsBroken = false;
        Durability = Entry.Durability;
        m_RegenRemain = 0f;
        m_GrowRemain = 0f;
    }

    // 고른 소켓에 앞에서부터 꽂는다. 남는 버섯은 숨긴다
    public void Place(IReadOnlyList<Transform> listSocket)
    {
        m_PlacedCount = Mathf.Min(listSocket.Count, m_ListMushroom.Count);
        for (int i = 0; i < m_ListMushroom.Count; i++)
        {
            Mushroom mushroom = m_ListMushroom[i];
            if (i >= m_PlacedCount)
            {
                mushroom.Detach();
                continue;
            }

            float tilt = Entry.TiltMax;
            Quaternion rotation = Quaternion.Euler(Random.Range(-tilt, tilt), Random.Range(-180f, 180f), Random.Range(-tilt, tilt));
            float scale = Random.Range(Entry.ScaleRange.x, Entry.ScaleRange.y);
            mushroom.Attach(listSocket[i], rotation, scale);
        }

        SetBrokenModel(false);
    }

    public override void Tick(float deltaTime)
    {
        if (m_GrowRemain > 0f)
            UpdateGrow(deltaTime);

        if (IsBroken == false || Entry.RegenTime <= 0f)
            return;

        m_RegenRemain -= deltaTime;
        if (m_RegenRemain > 0f)
            return;

        Regrow();
    }

    public void TakeDamage(int damage)
    {
        if (CanBreak == false)
            return;

        Durability = Mathf.Max(0, Durability - damage);
        if (Durability > 0)
            return;

        IsBroken = true;
        m_RegenRemain = Entry.RegenTime;

        SetBrokenModel(true);
        Broken?.Invoke(this);
    }

    // ==================================================

    private void Regrow()
    {
        IsBroken = false;
        Durability = Entry.Durability;
        m_GrowRemain = RegrowTime;

        SetNormalScale(RegrowStartScale);
        SetBrokenModel(false);
    }

    private void UpdateGrow(float deltaTime)
    {
        m_GrowRemain = Mathf.Max(0f, m_GrowRemain - deltaTime);

        float t = 1f - m_GrowRemain / RegrowTime;
        SetNormalScale(Mathf.Lerp(RegrowStartScale, 1f, t));
    }

    private void SetNormalScale(float rate)
    {
        for (int i = 0; i < m_PlacedCount; i++)
        {
            Mushroom mushroom = m_ListMushroom[i];
            mushroom.Normal.transform.localScale = mushroom.NormalScale * rate;
        }
    }

    private void SetBrokenModel(bool isBroken)
    {
        for (int i = 0; i < m_PlacedCount; i++)
        {
            Mushroom mushroom = m_ListMushroom[i];
            mushroom.Normal.gameObject.SetActive(isBroken == false);
            if (mushroom.Broken != null)
                mushroom.Broken.gameObject.SetActive(isBroken);
        }
    }
}
