using System.Collections.Generic;
using UnityEngine;

// 하얀 번쩍 반응. 잠깐 몸 렌더러의 머티리얼을 흰색 단색으로 바꿨다가 되돌린다.
// 히트스톱 중에도 끝나도록 주인이 실제 시간(unscaled)으로 Tick을 부른다
public class FlashFeature : IHitReaction
{
    private const float Duration = 0.08f;

    private static Material s_FlashMaterial;

    private readonly List<Renderer> m_ListRenderer = new();
    private readonly List<Material[]> m_ListOriginal = new();
    private float m_Remain;

    // ==================================================

    public FlashFeature(List<Renderer> listRenderer)
    {
        foreach (Renderer meshRenderer in listRenderer)
        {
            AddRenderer(meshRenderer);
        }
    }

    // 몸에 꽂은 버섯처럼 나중에 붙인 렌더러를 더한다
    public void AddRenderer(Renderer meshRenderer)
    {
        if (meshRenderer == null)
            return;

        m_ListRenderer.Add(meshRenderer);
        m_ListOriginal.Add(meshRenderer.sharedMaterials);
    }

    public void OnHit(in HitParams hit)
    {
        Material flash = GetFlashMaterial();
        for (int i = 0; i < m_ListRenderer.Count; i++)
        {
            if (m_ListRenderer[i] == null)
                continue;

            Material[] flashMaterials = new Material[m_ListOriginal[i].Length];
            for (int k = 0; k < flashMaterials.Length; k++)
            {
                flashMaterials[k] = flash;
            }

            m_ListRenderer[i].sharedMaterials = flashMaterials;
        }

        m_Remain = Duration;
    }

    public void Tick(float unscaledDeltaTime)
    {
        if (m_Remain <= 0f)
            return;

        m_Remain -= unscaledDeltaTime;
        if (m_Remain <= 0f)
            Restore();
    }

    public void Restore()
    {
        m_Remain = 0f;
        for (int i = 0; i < m_ListRenderer.Count; i++)
        {
            if (m_ListRenderer[i] == null)
                continue;

            m_ListRenderer[i].sharedMaterials = m_ListOriginal[i];
        }
    }

    private static Material GetFlashMaterial()
    {
        if (s_FlashMaterial == null)
            s_FlashMaterial = HitMaterials.CreateUnlit("HitFlash (Runtime)", Color.white);

        return s_FlashMaterial;
    }
}
