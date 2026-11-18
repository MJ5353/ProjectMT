using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 맞는 쪽 입구이자 피격 반응 처리. 팀·무적·사망을 거른 뒤 체력을 깎고, HitDef에서 켠 반응을 정해진 순서로 처리한다.
// 순서: 부위 → 체력 → 넉백 → 번쩍 → 히트스톱 → 조각·타격 마크
// 번쩍이 히트스톱 중에도 끝나야 해서 실제 시간(unscaled)으로 돌고, 무적 시간만 게임 시간으로 바꿔 깎는다

public class HitFeature : UnitFeature
{
    private const float ChunkHeight = 0.5f;   // 조각이 튀어나오는 높이

    private readonly ETeam m_Team;
    private readonly HitDef m_HitDef;
    private readonly HealthFeature m_Health;
    private readonly MoveFeature m_Move;

    // 번쩍 ---
    private readonly List<Renderer> m_ListFlashRenderer = new();
    private readonly List<Material[]> m_ListOriginalMaterial = new();
    private readonly List<Material[]> m_ListFlashMaterial = new();
    private readonly List<int> m_ListFlashPart = new(); // 렌더러마다 부위 번호. -1이면 몸
    private static readonly int s_BaseColorID = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock s_FlashBlock; // 번쩍 색. 같은 흰 머티리얼에 색만 얹는다

    // 부위 ---
    private PartFeature m_Parts = null; // 버섯 부위가 있는 유닛(몬스터)만

    // flag ---
    private float m_InvincibleRemain;
    private float m_FlashRemain;

    // hit ---
    public event Action<HitParams> Hit;
    
    // get ---
    public ETeam Team => m_Team;
    public bool IsDead => m_Health.IsDead;
    public bool IsInvincible => m_InvincibleRemain > 0f;
    public override bool IsUnscaledTime => true;

    // ------------------------------------------------

    public HitFeature(IActor owner, ETeam team, HitDef hitDef, HealthFeature health, MoveFeature move, List<Renderer> listFlashRenderer) : base(owner)
    {
        m_Team = team;
        m_HitDef = hitDef;
        m_Health = health;
        m_Move = move;

        foreach (Renderer meshRenderer in listFlashRenderer)
        {
            AddFlashRenderer(meshRenderer);
        }
    }

    public override void Reset()
    {
        m_InvincibleRemain = 0f;
        RestoreFlash();
    }

    public override void Tick(float unscaledDeltaTime)
    {
        // 무적은 게임 시간 기준. 히트스톱(timeScale 0) 중에는 같이 멈춘다
        if (m_InvincibleRemain > 0f)
            m_InvincibleRemain -= unscaledDeltaTime * Time.timeScale;

        if (m_FlashRemain <= 0f)
            return;

        m_FlashRemain -= unscaledDeltaTime;
        if (m_FlashRemain <= 0f)
            RestoreFlash();
    }

    public override void Clear()
    {
        RestoreFlash();
    }

    // 대시 같은 무적 구간. 남은 무적보다 길 때만 늘린다
    public void SetInvincible(float duration)
    {
        m_InvincibleRemain = Mathf.Max(m_InvincibleRemain, duration);
    }

    // 맞았으면 true
    public bool Receive(in HitParams hit)
    {
        if (hit.Team == m_Team)
            return false;

        if (IsDead || IsInvincible)
            return false;

        // 부위를 맞으면 부위 내구도와 본체 체력이 함께 깎인다. 본체 쪽은 부위의 몸 피해 배율을 곱한다
        // 깨졌거나 자라는 중이거나 장식인 부위는 몸으로 친다(번쩍도 몸)
        
        int bodyDamage = hit.Damage;
        int partIndex = -1;
        if (m_Parts != null && m_Parts.TryHit(hit.PartIndex, hit.Damage, out float bodyDamageRate))
        {
            partIndex = hit.PartIndex;
            bodyDamage = Mathf.RoundToInt(hit.Damage * bodyDamageRate);
        }

        m_Health.TakeDamage(bodyDamage);
        ApplyReactions(hit, partIndex);

        if (m_HitDef != null)
            m_InvincibleRemain = m_HitDef.InvincibleTime;

        Hit?.Invoke(hit);

        return true;
    }

    public void SetParts(PartFeature parts)
    {
        m_Parts = parts;
    }

    // 번쩍 ---

    // 몸에 꽂은 버섯처럼 나중에 붙인 렌더러를 더한다. 부위 렌더러는 그 부위를 맞았을 때만 번쩍인다
    public void AddFlashRenderer(Renderer meshRenderer, int partIndex = -1)
    {
        if (meshRenderer == null)
            return;

        m_ListFlashRenderer.Add(meshRenderer);
        m_ListOriginalMaterial.Add(meshRenderer.sharedMaterials);
        m_ListFlashMaterial.Add(CreateFlashMaterials(meshRenderer.sharedMaterials.Length));
        m_ListFlashPart.Add(partIndex);
    }

    public void RestoreFlash()
    {
        m_FlashRemain = 0f;
        for (int i = 0; i < m_ListFlashRenderer.Count; i++)
        {
            if (m_ListFlashRenderer[i] == null)
                continue;

            m_ListFlashRenderer[i].sharedMaterials = m_ListOriginalMaterial[i];
            m_ListFlashRenderer[i].SetPropertyBlock(null);
        }
    }

    // 반응---

    private void ApplyReactions(in HitParams hit, int partIndex)
    {
        if (m_HitDef == null)
            return;

        if (m_HitDef.Has(EHitReaction.Knockback))
            ApplyKnockback(hit);

        if (m_HitDef.Has(EHitReaction.Flash))
            ApplyFlash(partIndex);

        if (m_HitDef.Has(EHitReaction.HitStop))
            ApplyHitStop(hit);

        if (m_HitDef.Has(EHitReaction.ChunkBurst))
            ApplyChunkBurst(hit, partIndex);
    }

    private void ApplyKnockback(in HitParams hit)
    {
        if (m_HitDef.KnockbackScale <= 0f)
            return;

        m_Move.AddImpulse(hit.Direction * (hit.Knockback * m_HitDef.KnockbackScale));
    }

    // 몸을 맞으면 몸 렌더러만, 부위를 맞으면 그 부위 버섯만 번쩍인다
    private void ApplyFlash(int partIndex)
    {
        if (HitMaterials.Flash == null)
            return;

        RestoreFlash(); // 다른 부위가 번쩍이던 중이면 먼저 되돌린다
        s_FlashBlock ??= new MaterialPropertyBlock();
        s_FlashBlock.SetColor(s_BaseColorID, partIndex < 0 ? m_HitDef.FlashColor : m_HitDef.PartFlashColor);
        for (int i = 0; i < m_ListFlashRenderer.Count; i++)
        {
            if (m_ListFlashRenderer[i] == null || m_ListFlashPart[i] != partIndex)
                continue;

            m_ListFlashRenderer[i].sharedMaterials = m_ListFlashMaterial[i];
            m_ListFlashRenderer[i].SetPropertyBlock(s_FlashBlock);
        }

        m_FlashRemain = m_HitDef.FlashTime;
    }

    private void ApplyHitStop(in HitParams hit)
    {
        GameManager.Instance?.HitStopS?.Request(hit.HitStop);
    }

    // 전투 씬이 아니면 조각을 튀기지 않는다. 타격 마크는 실제 맞은 자리(부위 표면)에 띄운다
    private void ApplyChunkBurst(in HitParams hit, int partIndex)
    {
        ChunkBurstService chunkBurstS = GameManager.Instance?.ChunkS;
        if (chunkBurstS == null)
            return;

        chunkBurstS.Burst(Owner.transform.position + Vector3.up * ChunkHeight, hit.Direction, m_HitDef.ChunkColor);
        chunkBurstS.Mark(hit.Point, partIndex >= 0);
    }

    // 렌더러 머티리얼 칸 수만큼 번쩍 머티리얼을 채운 배열. 맞을 때마다 새로 만들지 않도록 미리 만든다
    private static Material[] CreateFlashMaterials(int count)
    {
        Material flash = HitMaterials.Flash;
        Material[] flashMaterials = new Material[count];
        for (int i = 0; i < count; i++)
        {
            flashMaterials[i] = flash;
        }

        return flashMaterials;
    }
}
