using System;
using UnityEngine;

// ==================================================

// 몸에 꽂힌 버섯 부위 하나. 내구도가 0이 되면 깨진 모델로 바꾸고, 재생 시간 뒤 작게 다시 돋아 원래 크기로 자란다.
// 일반·깨진 모델을 둘 다 소켓에 붙여 두고 켜고 끈다(새로 만들지 않아 풀이 필요 없다)
// [mj] todo) 이거 하나로 안됨?

public class PartFeature : UnitFeature
{
    private const float RegrowTime = 1.5f;         // [mj] todo) 임시값. 다시 돋은 뒤 원래 크기까지 자라는 시간(초)
    private const float RegrowStartScale = 0.3f;   // 다시 돋을 때 처음 크기 배율
    private readonly Renderer m_Normal;
    private readonly Renderer m_Broken;
    private readonly Vector3 m_NormalScale;

    public int Index { get; } // MonsterDef.ListMushroom 칸 번호 = HitParams.PartIndex
    public MonsterDef.MushroomSocket Socket { get; }
    public Transform Bone { get; }
    public int Durability { get; private set; }
    public bool IsBroken { get; private set; }
    public bool CanBreak => Socket.IsBreakable && IsBroken == false && m_GrowRemain <= 0f; 

    // runtime ---
    
    private float m_RegenRemain;
    private float m_GrowRemain;

    // 자라는 중에는 깨지지 않는다
    public event Action<PartFeature> Broken;

    // ------------------------------------------------

    public PartFeature(IActor owner, int index, MonsterDef.MushroomSocket socket, Transform bone, Renderer normal, Renderer broken) : base(owner)
    {
        Index = index;
        Socket = socket;
        Bone = bone;
        m_Normal = normal;
        m_Broken = broken;
        m_NormalScale = normal.transform.localScale;
    }

    // 풀에서 다시 꺼낼 때 원상복구
    public override void Reset()
    {
        IsBroken = false;
        Durability = Socket.Durability;
        m_RegenRemain = 0f;
        m_GrowRemain = 0f;

        m_Normal.transform.localScale = m_NormalScale;
        SetBrokenModel(false);
    }

    public override void Tick(float deltaTime)
    {
        if (m_GrowRemain > 0f)
            UpdateGrow(deltaTime);

        if (IsBroken == false || Socket.RegenTime <= 0f)
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
        m_RegenRemain = Socket.RegenTime;
        
        SetBrokenModel(true);
        Broken?.Invoke(this);
    }

    // ==================================================

    private void Regrow()
    {
        IsBroken = false;
        Durability = Socket.Durability;
        m_GrowRemain = RegrowTime;

        m_Normal.transform.localScale = m_NormalScale * RegrowStartScale;
        SetBrokenModel(false);
    }

    private void UpdateGrow(float deltaTime)
    {
        m_GrowRemain = Mathf.Max(0f, m_GrowRemain - deltaTime);
        
        float t = 1f - m_GrowRemain / RegrowTime;
        m_Normal.transform.localScale = m_NormalScale * Mathf.Lerp(RegrowStartScale, 1f, t);
    }

    private void SetBrokenModel(bool isBroken)
    {
        m_Normal.gameObject.SetActive(isBroken == false);

        if (m_Broken != null)
            m_Broken.gameObject.SetActive(isBroken);
    }
}
