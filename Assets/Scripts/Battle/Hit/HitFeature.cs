using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 맞는 쪽 입구이자 피격 반응 처리. 팀·무적·사망을 거른 뒤 체력을 깎고, HitDef에서 켠 반응을 정해진 순서로 처리한다.
// 순서: 체력 → 넉백 → 번쩍 → 히트스톱 → 조각
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

    // flag
    private float m_InvincibleRemain;
    private float m_FlashRemain;

    public ETeam Team => m_Team;
    public bool IsDead => m_Health.IsDead;
    public bool IsInvincible => m_InvincibleRemain > 0f;
    public override bool IsUnscaledTime => true;

    public event Action<HitParams> Hit;

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

        m_Health.TakeDamage(hit.Damage);
        ApplyReactions(hit);

        if (m_HitDef != null)
            m_InvincibleRemain = m_HitDef.InvincibleTime;

        Hit?.Invoke(hit);

        return true;
    }

    // 번쩍 렌더러---

    // 몸에 꽂은 버섯처럼 나중에 붙인 렌더러를 더한다
    public void AddFlashRenderer(Renderer meshRenderer)
    {
        if (meshRenderer == null)
            return;

        m_ListFlashRenderer.Add(meshRenderer);
        m_ListOriginalMaterial.Add(meshRenderer.sharedMaterials);
        m_ListFlashMaterial.Add(CreateFlashMaterials(meshRenderer.sharedMaterials.Length));
    }

    // 버섯이 깨진 모델로 바뀔 때처럼 렌더러를 갈아 끼운다. 번쩍 중이면 새 렌더러도 바로 번쩍인다
    public void ReplaceFlashRenderer(Renderer oldRenderer, Renderer newRenderer)
    {
        int index = m_ListFlashRenderer.IndexOf(oldRenderer);
        if (index < 0)
        {
            AddFlashRenderer(newRenderer);
            return;
        }

        if (m_FlashRemain > 0f && oldRenderer != null)
            oldRenderer.sharedMaterials = m_ListOriginalMaterial[index];

        if (newRenderer == null)
        {
            m_ListFlashRenderer.RemoveAt(index);
            m_ListOriginalMaterial.RemoveAt(index);
            m_ListFlashMaterial.RemoveAt(index);
            return;
        }

        m_ListFlashRenderer[index] = newRenderer;
        m_ListOriginalMaterial[index] = newRenderer.sharedMaterials;
        m_ListFlashMaterial[index] = CreateFlashMaterials(newRenderer.sharedMaterials.Length);

        if (m_FlashRemain > 0f)
            newRenderer.sharedMaterials = m_ListFlashMaterial[index];
    }

    public void RestoreFlash()
    {
        m_FlashRemain = 0f;
        for (int i = 0; i < m_ListFlashRenderer.Count; i++)
        {
            if (m_ListFlashRenderer[i] == null)
                continue;

            m_ListFlashRenderer[i].sharedMaterials = m_ListOriginalMaterial[i];
        }
    }

    // ==================================================

    // 반응---

    private void ApplyReactions(in HitParams hit)
    {
        if (m_HitDef == null)
            return;

        if (m_HitDef.Has(EHitReaction.Knockback))
            ApplyKnockback(hit);

        if (m_HitDef.Has(EHitReaction.Flash))
            ApplyFlash();

        if (m_HitDef.Has(EHitReaction.HitStop))
            ApplyHitStop(hit);

        if (m_HitDef.Has(EHitReaction.ChunkBurst))
            ApplyChunkBurst(hit);
    }

    private void ApplyKnockback(in HitParams hit)
    {
        if (m_HitDef.KnockbackScale <= 0f)
            return;

        m_Move.AddImpulse(hit.Direction * (hit.Knockback * m_HitDef.KnockbackScale));
    }

    private void ApplyFlash()
    {
        if (HitMaterials.Flash == null)
            return;

        for (int i = 0; i < m_ListFlashRenderer.Count; i++)
        {
            if (m_ListFlashRenderer[i] == null)
                continue;

            m_ListFlashRenderer[i].sharedMaterials = m_ListFlashMaterial[i];
        }

        m_FlashRemain = m_HitDef.FlashTime;
    }

    private void ApplyHitStop(in HitParams hit)
    {
        GameManager.Instance?.HitStop?.Request(hit.HitStop);
    }

    // 전투 씬이 아니면 조각을 튀기지 않는다
    private void ApplyChunkBurst(in HitParams hit)
    {
        ChunkBurstService chunkBurstS = GameManager.Instance?.Chunks;
        if (chunkBurstS == null)
            return;

        chunkBurstS.Burst(Owner.transform.position + Vector3.up * ChunkHeight, hit.Direction, m_HitDef.ChunkColor);
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
