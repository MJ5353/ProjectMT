using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 맞는 쪽 전부. 어디를 맞을 수 있나(몸 판정 모양)와 맞았을 때 처리(팀·무적·사망 거르기 → 체력 → HitDef 반응)를 맡는다.
// 판정 모양: 뼈마다 구·캡슐. 목록이 비면 HurtBody 캡슐로 맞는다(플레이어·잡몹). 버섯 부위가 걸린 뼈는 부위 번호를 달아 부위 판정이 된다
// 순서: 부위 → 체력 → 넉백 → 번쩍 → 히트스톱 → 조각·타격 마크
// 경직: 맞을 때마다 Break를 쌓아 UnitDef.BreakImmune을 넘고 판단이 허락(CanFlinch)하면 경직(넉백·피격 행동). 아니면 넉백 없이 데미지·번쩍만
// 번쩍이 히트스톱 중에도 끝나야 해서 실제 시간(unscaled)으로 돌고, 무적 시간만 게임 시간으로 바꿔 깎는다

public class HitFeature : UnitFeature
{
    private const float ChunkHeight = 0.5f; // 조각이 튀어나오는 높이
    private const float BoundMargin = 1.3f; // 애니로 뼈가 뻗는 만큼 감싸는 반지름에 더 주는 배율

    private readonly HitDef m_HitDef;
    private readonly List<HitShape> m_ListShape = new(); // 만들 때 한 번 정하고 풀 재사용 때도 그대로

    public event Action<HitParams, bool> Hit; // bool: 경직하는 타격인지
    public float BoundRadius { get; private set; } // 몸 중심(transform)에서 모양들을 감싸는 반지름. 후보 거르기용

    // 번쩍
    private readonly List<Renderer> m_ListFlashRenderer = new();
    private readonly List<Material[]> m_ListOriginalMaterial = new();
    private readonly List<Material[]> m_ListFlashMaterial = new();
    private readonly List<int> m_ListFlashPart = new(); // 렌더러마다 부위 번호. -1이면 몸
    private static readonly int s_BaseColorID = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock s_FlashBlock; // 번쩍 색. 같은 흰 머티리얼에 색만 얹는다

    // flag
    private float m_InvincibleRemain;
    private float m_FlashRemain;
    private float m_BreakStack; // 쌓인 경직
    private float m_BreakIdleTime; // 마지막 타격 뒤 지난 게임 시간

    // get
    public bool IsInvincible => m_InvincibleRemain > 0f;
    public bool IsHidden { get; private set; } // 숨음(땅속 등): 맞지 않고 대상 찾기·조준에서도 빠진다
    public IReadOnlyList<HitShape> ListShape => m_ListShape;
    public bool HasShape => m_ListShape.Count > 0;
    public override bool IsUnscaledTime => true;

    // ------------------------------------------------

    public HitFeature(UnitController owner, HitDef hitDef, List<Renderer> listFlashRenderer) : base(owner)
    {
        m_HitDef = hitDef;

        foreach (Renderer meshRenderer in listFlashRenderer)
        {
            AddFlashRenderer(meshRenderer);
        }
    }

    public override void Reset()
    {
        m_InvincibleRemain = 0f;
        m_BreakStack = 0f;
        IsHidden = false;
        RestoreFlash();
    }

    public override void Tick(float unscaledDeltaTime)
    {
        // 무적은 게임 시간 기준. 히트스톱(timeScale 0) 중에는 같이 멈춘다
        if (m_InvincibleRemain > 0f)
            m_InvincibleRemain -= unscaledDeltaTime * Time.timeScale;

        if (m_BreakStack > 0f)
        {
            m_BreakIdleTime += unscaledDeltaTime * Time.timeScale;
            if (m_BreakIdleTime >= Owner.Def.BreakResetTime)
                m_BreakStack = 0f;
        }

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

    // 몸 판정 모양을 만든다. 부위 번호를 쓰므로 PartFeature를 만든 뒤 주인이 부른다. 감싸는 반지름은 지금(만들 때) 자세 기준
    // shortcut: 애니로 뼈가 BoundMargin보다 멀리 뻗으면 후보에서 빠진다. 그러면 매 프레임 재계산으로 바꾼다
    public void SetShapes(IReadOnlyList<HitShapeDef> listShapeDef)
    {
        m_ListShape.Clear();
        BoundRadius = 0f;

        Vector3 center = Owner.transform.position;
        foreach (HitShapeDef shapeDef in listShapeDef)
        {
            Transform bone = Owner.Property.GetBattleBone(shapeDef.Part);
            if (bone == null)
            {
                CLogger.Warning("HitFeature", $"판정 부위 뼈가 프리팹에 없음: {shapeDef.Part} ({Owner.transform.name})", Owner.transform);
                continue;
            }

            int partIndex = Owner.PartF != null ? Owner.PartF.FindIndex(shapeDef.Part) : -1;
            HitShape shape = new HitShape(bone, shapeDef, partIndex);
            m_ListShape.Add(shape);

            shape.GetSegment(out Vector3 start, out Vector3 end);
            float reach = Mathf.Max((start - center).magnitude, (end - center).magnitude) + shape.Radius;
            BoundRadius = Mathf.Max(BoundRadius, reach * BoundMargin);
        }
    }

    // 대시 같은 무적 구간. 남은 무적보다 길 때만 늘린다
    public void SetInvincible(float duration)
    {
        m_InvincibleRemain = Mathf.Max(m_InvincibleRemain, duration);
    }

    // 숨기(AIHideState). 무적과 달리 대상 찾기·조준(UnitService)에서도 빠진다
    public void SetHidden(bool isHidden)
    {
        IsHidden = isHidden;
    }

    // 맞았으면 true
    public bool Receive(in HitParams hit)
    {
        if (hit.Team == Owner.Team)
            return false;

        if (Owner.IsDead || IsInvincible || IsHidden || Owner.Def.IsUntargetable)
            return false;

        // 부위를 맞으면 부위 내구도와 본체 체력이 함께 깎인다. 본체 쪽은 부위의 몸 피해 배율을 곱한다
        // 깨졌거나 자라는 중이거나 장식인 부위는 몸으로 친다(번쩍도 몸)

        int bodyDamage = hit.Damage;
        int partIndex = -1;
        PartFeature parts = Owner.PartF; // 버섯 부위가 있는 유닛(몬스터)만
        if (parts != null && parts.TryHit(hit.PartIndex, hit.Damage, out float bodyDamageRate))
        {
            partIndex = hit.PartIndex;
            bodyDamage = Mathf.RoundToInt(hit.Damage * bodyDamageRate);
        }

        bool isFlinch = AddBreak(hit.Break);
        Owner.HealthF.TakeDamage(bodyDamage);
        ApplyReactions(hit, partIndex, isFlinch);

        if (m_HitDef != null)
            m_InvincibleRemain = m_HitDef.InvincibleTime;

        Hit?.Invoke(hit, isFlinch);

        return true;
    }

    // 경직 판단이 막으면(쓰러짐·포효) 쌓지도 않는다. 넘으면 비운다
    private bool AddBreak(float breakPower)
    {
        if (Owner.BrainBase != null && Owner.BrainBase.CanFlinch == false)
            return false;

        m_BreakStack += breakPower;
        m_BreakIdleTime = 0f;

        if (m_BreakStack <= Owner.Def.BreakImmune)
            return false;

        m_BreakStack = 0f;
        return true;
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

    private void ApplyReactions(in HitParams hit, int partIndex, bool isFlinch)
    {
        if (m_HitDef == null)
            return;

        if (isFlinch && m_HitDef.Has(EHitReaction.Knockback))
            ApplyKnockback(hit);

        if (m_HitDef.Has(EHitReaction.Flash))
            ApplyFlash(partIndex);

        if (m_HitDef.Has(EHitReaction.HitStop))
            ApplyHitStop(hit);

        if (m_HitDef.Has(EHitReaction.ChunkBurst))
            ApplyChunkBurst(hit, partIndex);

        if (m_HitDef.Has(EHitReaction.CameraShake))
            ApplyCameraShake(hit);
    }

    private void ApplyKnockback(in HitParams hit)
    {
        if (m_HitDef.KnockbackScale <= 0f)
            return;

        Owner.MoveF.AddImpulse(hit.Direction * (hit.Knockback * m_HitDef.KnockbackScale));
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

    private void ApplyCameraShake(in HitParams hit)
    {
        CameraManager.Instance?.Shake(hit.Shake, m_HitDef.ShakeTime);
    }

    // 전투 씬이 아니면 조각을 튀기지 않는다. 타격 마크는 실제 맞은 자리(부위 표면)에 띄운다
    private void ApplyChunkBurst(in HitParams hit, int partIndex)
    {
        EffectService effectS = GameManager.Instance?.EffectS;
        if (effectS == null)
            return;

        effectS.Burst(Owner.transform.position + Vector3.up * ChunkHeight, hit.Direction, m_HitDef.ChunkColor);
        effectS.Mark(hit.Point, partIndex >= 0);
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