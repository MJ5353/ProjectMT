using System;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

// ==================================================

// 몸에 꽂힌 버섯 부위 전부(MonsterDef.ListPart). 유닛당 하나.
// 버섯 모델은 만들 때 부위마다 최대 개수만큼 미리 만들어 두고, 스폰(Reset)마다 소켓을 다시 골라 옮긴다(풀이 필요 없다).
// 부위 내구도가 0이 되면 꽂힌 버섯이 같이 깨진 모델로 바뀌고, 재생 시간 뒤 작게 다시 돋아 원래 크기로 자란다

public class PartFeature : UnitFeature
{
    private static DefineDef Define => GameManager.Instance.Define; // 조절값(Define.asset)

    private readonly IReadOnlyList<MonsterDef.PartEntry> m_ListEntry; // Def 데이터(읽기 전용)
    private readonly List<Part> m_ListPart = new();               // [i] = MonsterDef.ListPart 칸. 버섯이 없는 칸은 null
    private readonly HashSet<Transform> m_SetUsedSocket = new();  // 스폰 때 버섯 배치용
    private readonly List<Transform> m_ListSocketPick = new();

    public IReadOnlyList<Part> ListPart => m_ListPart;
    public event Action<Part> Broken;   // 드롭은 여기서 끝낸 뒤 알린다(휘청 같은 다른 반응용)

    // ------------------------------------------------
    
    // 꽂힌 버섯 하나. 일반·깨진 모델을 둘 다 소켓에 붙여 두고 켜고 끈다
    public class Mushroom
    {
        public readonly Renderer Normal;
        public readonly Renderer Broken;                    // 없으면 깨질 때 사라진다
        
        public Transform Socket { get; private set; }       // 이번 스폰에 꽂힌 소켓. null이면 안 꽂힘
        public Vector3 NormalScale { get; private set; }

        // ------------------------------------------------
        
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

    // 부위 하나(MonsterDef.PartEntry)의 상태와 꽂힌 버섯들
    public class Part
    {
        public readonly int Index; // MonsterDef.ListPart 칸 번호 = HitParams.PartIndex
        public readonly MonsterDef.PartEntry Entry;
        public IReadOnlyList<Mushroom> ListMushroom => m_ListMushroom;
        public int PlacedCount { get; private set; } // 이번 스폰에 꽂힌 버섯 수(ListMushroom 앞에서부터)
        public int Durability { get; private set; }
        public bool IsBroken { get; private set; }
        public bool CanBreak => Entry.IsBreakable && IsBroken == false && m_GrowRemain <= 0f && PlacedCount > 0;

        private readonly List<Mushroom> m_ListMushroom = new();
        private float m_RegenRemain;
        private float m_GrowRemain;  // 자라는 중에는 깨지지 않는다

        // ------------------------------------------------

        public Part(int index, MonsterDef.PartEntry entry)
        {
            Index = index;
            Entry = entry;
        }

        public void AddMushroom(Renderer normal, Renderer broken)
        {
            m_ListMushroom.Add(new Mushroom(normal, broken));
        }

        public void Reset()
        {
            IsBroken = false;
            Durability = Entry.Durability;
            m_RegenRemain = 0f;
            m_GrowRemain = 0f;
        }

        // 고른 소켓에 앞에서부터 꽂는다. 남는 버섯은 숨긴다
        public void Place(IReadOnlyList<Transform> listSocket)
        {
            PlacedCount = Mathf.Min(listSocket.Count, m_ListMushroom.Count);
            for (int i = 0; i < m_ListMushroom.Count; i++)
            {
                Mushroom mushroom = m_ListMushroom[i];
                if (i >= PlacedCount)
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

        public void Tick(float deltaTime)
        {
            if (m_GrowRemain > 0f)
                UpdateGrow(deltaTime);

            if (IsBroken == false || Entry.RegenTime <= 0f)
                return;

            m_RegenRemain -= deltaTime;
            if (m_RegenRemain <= 0f)
                Regrow();
        }

        // 내구도를 깎는다. 이번에 깨졌으면 true
        public bool TakeDamage(int damage)
        {
            Durability = Mathf.Max(0, Durability - damage);
            if (Durability > 0)
                return false;

            IsBroken = true;
            m_RegenRemain = Entry.RegenTime;
            
            SetBrokenModel(true);
            return true;
        }

        private void Regrow()
        {
            IsBroken = false;
            Durability = Entry.Durability;
            m_GrowRemain = Define.Part.RegrowTime;

            SetNormalScale(Define.Part.RegrowStartScale);
            SetBrokenModel(false);
        }

        private void UpdateGrow(float deltaTime)
        {
            m_GrowRemain = Mathf.Max(0f, m_GrowRemain - deltaTime);

            float t = 1f - m_GrowRemain / Define.Part.RegrowTime;
            SetNormalScale(Mathf.Lerp(Define.Part.RegrowStartScale, 1f, t));
        }

        private void SetNormalScale(float rate)
        {
            for (int i = 0; i < PlacedCount; i++)
            {
                Mushroom mushroom = m_ListMushroom[i];
                mushroom.Normal.transform.localScale = mushroom.NormalScale * rate;
            }
        }

        private void SetBrokenModel(bool isBroken)
        {
            for (int i = 0; i < PlacedCount; i++)
            {
                Mushroom mushroom = m_ListMushroom[i];
                mushroom.Normal.gameObject.SetActive(isBroken == false);
                
                if (mushroom.Broken != null)
                    mushroom.Broken.gameObject.SetActive(isBroken);
            }
        }
    }

    // ------------------------------------------------

    // 부위마다 버섯 모델을 최대 개수만큼 미리 만든다. 맞은 부위만 번쩍이도록 부위 번호와 함께 렌더러를 넘긴다
    public PartFeature(UnitController owner, IReadOnlyList<MonsterDef.PartEntry> listEntry) : base(owner)
    {
        m_ListEntry = listEntry;
        for (int i = 0; i < listEntry.Count; i++)
        {
            m_ListPart.Add(CreatePart(i, listEntry[i], owner));
        }
    }

    // 풀에서 다시 꺼낼 때 원상복구하고 소켓을 새로 골라 꽂는다
    public override void Reset()
    {
        foreach (Part part in m_ListPart)
        {
            part?.Reset();
        }

        PlaceMushrooms();
    }

    public override void Tick(float deltaTime)
    {
        foreach (Part part in m_ListPart)
        {
            part?.Tick(deltaTime);
        }
    }

    // 맞은 부위가 깰 수 있으면 내구도를 깎고 본체 피해 배율을 넘긴다. 못 깨면 false(몸으로 친다)
    public bool TryHit(int index, int damage, out float bodyDamageRate)
    {
        bodyDamageRate = 1f;
        
        Part part = GetPart(index);
        if (part == null || part.CanBreak == false)
            return false;

        bodyDamageRate = part.Entry.BodyDamageRate;
        
        if (part.TakeDamage(damage))
        {
            PlayBreakEffect(part);
            SpawnDrops(part);
            Broken?.Invoke(part);
        }

        return true;
    }

    // ---
    
    // 꽂힌 버섯 자리마다 깨짐 이펙트
    private static void PlayBreakEffect(Part part)
    {
        EffectService effectS = FlowManager.Instance.EffectS;
        if (effectS == null)
            return;

        for (int i = 0; i < part.PlacedCount; i++)
        {
            effectS.Play(ECommonEffectType.PartBreak, part.ListMushroom[i].Socket.position, Quaternion.identity);
        }
    }

    // 꽂힌 버섯 하나마다 확정 드롭. 살아 있을 때 깨면 상급, 죽은 뒤면 하급
    private void SpawnDrops(Part part)
    {
        if (part.Entry.Mushroom == null)
            return;

        LootService lootS = FlowManager.Instance.Battle?.LootS;
        MushroomPickup prefab = ResourceManager.Instance.Load<MushroomPickup>(part.Entry.Mushroom.PickupPath, "데이터 편집기 > 버섯 > 픽업 빌드");
        
        if (lootS == null || prefab == null)
            return;

        EFreshness freshness = Owner.IsDead ? EFreshness.Low : EFreshness.High;
        for (int i = 0; i < part.PlacedCount; i++)
        {
            Vector3 position = part.ListMushroom[i].Socket.position;
            MushroomPickup pickup = lootS.SpawnPickup(prefab, position + Vector3.up * Define.Drop.PartDrop.Height);
            
            if (pickup == null)
                return;

            Vector2 spread = Random.insideUnitCircle * Define.Drop.PartDrop.Spread;
            pickup.Launch(new Vector3(spread.x, Define.Drop.PartDrop.UpSpeed, spread.y), freshness, LootService.RollRarity());
        }
    }

    private static Part CreatePart(int index, MonsterDef.PartEntry entry, UnitController owner)
    {
        if (entry.Model == null || entry.CountMax <= 0)
            return null;

        Part part = new Part(index, entry);
        for (int i = 0; i < entry.CountMax; i++)
        {
            Renderer normal = ResourceManager.S_Create(entry.Model, owner.transform);
            Renderer broken = ResourceManager.S_Create(entry.BrokenModel, owner.transform); // 깨진 모델이 없으면 null

            owner.HitF.AddFlashRenderer(normal, index);
            owner.HitF.AddFlashRenderer(broken, index);
            part.AddMushroom(normal, broken);
        }

        return part;
    }

    // 깨지는 부위 먼저, 장식(None)은 남은 소켓 어디든
    private void PlaceMushrooms()
    {
        m_SetUsedSocket.Clear();
        
        foreach (Part part in m_ListPart)
        {
            if (part != null && part.Entry.BattlePart != EBattlePartType.None)
                PlacePart(part);
        }

        foreach (Part part in m_ListPart)
        {
            if (part != null && part.Entry.BattlePart == EBattlePartType.None)
                PlacePart(part);
        }
    }

    private void PlacePart(Part part)
    {
        // 장식(None)이나 부위 소켓이 없는 칸(Hitbox 뼈가 없는 잡몹)은 몸 어디든
        m_ListSocketPick.Clear();
        
        UnitProperty.BonePart bonePart = Owner.Property.GetBonePart(part.Entry.BattlePart);
        if (bonePart != null && bonePart.ListSocket.Count > 0)
        {
            m_ListSocketPick.AddRange(bonePart.ListSocket);
        }
        else
        {
            foreach (UnitProperty.BonePart candidate in Owner.Property.ListBonePart)
            {
                m_ListSocketPick.AddRange(candidate.ListSocket);
            }
        }

        m_ListSocketPick.RemoveAll(socket => socket == null || m_SetUsedSocket.Contains(socket));

        // 앞에서부터 count개를 무작위로 뽑아 채운다(부분 셔플)
        int count = Mathf.Min(Random.Range(part.Entry.CountMin, part.Entry.CountMax + 1), m_ListSocketPick.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(i, m_ListSocketPick.Count);
            (m_ListSocketPick[i], m_ListSocketPick[pick]) = (m_ListSocketPick[pick], m_ListSocketPick[i]);
        }

        m_ListSocketPick.RemoveRange(count, m_ListSocketPick.Count - count);
        m_SetUsedSocket.UnionWith(m_ListSocketPick);
        part.Place(m_ListSocketPick);
    }
    
    // get --- 
    
    public Part GetPart(int index)
    {
        return index >= 0 && index < m_ListPart.Count ? m_ListPart[index] : null;
    }

    // 이 몸 부위를 BattlePart로 가진 깨지는 부위 칸. 없으면 -1. 같은 부위 칸이 둘이면 첫 칸(판정 모양에 부위 번호를 달 때)
    public int FindIndex(EBattlePartType battlePart)
    {
        if (battlePart == EBattlePartType.None)
            return -1;

        for (int i = 0; i < m_ListEntry.Count; i++)
        {
            if (m_ListEntry[i].BattlePart == battlePart && m_ListEntry[i].IsBreakable)
                return i;
        }

        return -1;
    }
}
