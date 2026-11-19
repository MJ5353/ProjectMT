using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 전투 이펙트 입구. 파티클 이펙트(EffectController)는 풀에서 꺼내 살아 있는 동안 들고 있다가 끝나면 돌려놓고,
// 피격 복셀 조각·타격 마크는 ChunkRenderer로 오브젝트 없이 그린다.
// 공격 이펙트는 데이터(AttackSpec.Effect), 부위 파괴·사망·줍기처럼 정해진 연출은 Resources/Prefabs/Effect의 고정 프리팹을 쓴다

public sealed class EffectService : ServiceBase
{
    private readonly List<EffectController> m_ListActive = new();
    private readonly ChunkRenderer m_Chunk = new();

    public EffectController PartBreak { get; private set; } // 부위(버섯) 깨짐
    public EffectController Death { get; private set; }     // 몬스터 사망. 발밑
    public EffectController Pickup { get; private set; }    // 버섯 줍기

    // ------------------------------------------------

    public override void Init()
    {
        PartBreak = Load(CommonPath.PartBreakEffect);
        Death = Load(CommonPath.DeathEffect);
        Pickup = Load(CommonPath.PickupEffect);
    }

    public override void Tick(float deltaTime)
    {
        for (int i = m_ListActive.Count - 1; i >= 0; i--)
        {
            EffectController effect = m_ListActive[i];
            if (effect != null && effect.IsAlive)
                continue;

            m_ListActive.RemoveAt(i);
            ResourceManager.Instance?.Despawn(effect);
        }

        m_Chunk.Tick(deltaTime);
    }

    public override void Clear()
    {
        foreach (EffectController effect in m_ListActive)
        {
            ResourceManager.Instance?.Despawn(effect);
        }

        m_ListActive.Clear();
        m_Chunk.Clear();
    }

    // ---
    
    // 풀에서 꺼내 재생한다. 크기는 프리팹 기준 배율
    public EffectController Play(EffectController prefab, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (prefab == null || ResourceManager.Instance == null)
            return null;

        EffectController effect = ResourceManager.Instance.Spawn(prefab, position, rotation);
        effect.transform.localScale = Vector3.one * scale;
        m_ListActive.Add(effect);
        return effect;
    }

    // origin에서 direction 쪽으로 복셀 조각을 흩뿌린다
    public void Burst(Vector3 origin, Vector3 direction, Color color)
    {
        m_Chunk.Burst(origin, direction, color);
    }

    // 맞은 자리에 도트 별 타격 마크. 부위면 크고 오래 남는다
    public void Mark(Vector3 point, bool isPart)
    {
        m_Chunk.Mark(point, isPart);
    }

    private static EffectController Load(string resourcePath)
    {
        return ResourceManager.Instance?.Load<EffectController>(resourcePath);
    }
}
