using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 하얀 번쩍 반응. 잠깐 모든 렌더러의 머티리얼을 흰색 단색으로 바꿨다가 되돌린다.
// 히트스톱 중에도 끝나도록 실제 시간으로 잰다.
public class FlashFeature : MonoBehaviour, IHitReaction
{
    [SerializeField] private float m_Duration = 0.08f;
    [SerializeField, Tooltip("비우면 URP Unlit 흰색을 만들어 쓴다")] private Material m_FlashMaterial = null;

    private static Material s_DefaultFlash;

    private readonly List<Renderer> m_ListRenderer = new();
    private readonly List<Material[]> m_ListOriginal = new();
    private Coroutine m_Routine;

    public int Order => 20;

    // ==================================================

    private void OnDisable()
    {
        Restore();
    }

    // 버섯처럼 나중에 붙인 렌더러까지 다시 모은다
    public void Refresh()
    {
        Restore();
        m_ListRenderer.Clear();
        m_ListOriginal.Clear();

        foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (meshRenderer is ParticleSystemRenderer)
                continue;

            m_ListRenderer.Add(meshRenderer);
            m_ListOriginal.Add(meshRenderer.sharedMaterials);
        }
    }

    public void OnHit(in HitParams hit)
    {
        if (m_ListRenderer.Count == 0)
            Refresh();

        if (m_Routine != null)
            StopCoroutine(m_Routine);

        m_Routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Material flash = m_FlashMaterial != null ? m_FlashMaterial : GetDefaultFlash();
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

        yield return new WaitForSecondsRealtime(m_Duration);
        Restore();
    }

    private void Restore()
    {
        m_Routine = null;
        for (int i = 0; i < m_ListRenderer.Count; i++)
        {
            if (m_ListRenderer[i] == null)
                continue;

            m_ListRenderer[i].sharedMaterials = m_ListOriginal[i];
        }
    }

    private static Material GetDefaultFlash()
    {
        if (s_DefaultFlash == null)
            s_DefaultFlash = HitMaterials.CreateUnlit("HitFlash (Runtime)", Color.white);

        return s_DefaultFlash;
    }
}
