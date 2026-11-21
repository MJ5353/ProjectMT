using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 전투 이펙트 입구. 파티클 이펙트(EffectController)는 풀에서 꺼내 살아 있는 동안 들고 있다가 끝나면 돌려놓고,
// 피격 복셀 조각·타격 마크는 ChunkRenderer로 오브젝트 없이 그린다.
// 공격 이펙트는 데이터(AttackSpec.Effect), 부위 파괴·사망·줍기처럼 정해진 연출은 ECommonEffectType으로 부른다(프리팹 FX_{타입 이름})

public sealed class EffectService : ServiceBase
{
    private readonly List<EffectController> m_ListActive = new();
    private readonly List<TelegraphController> m_ListTelegraph = new(); // 켜져 있는 공격 예고. 끄는 건 부른 쪽(AttackAction)
    private readonly List<EffectController> m_ListShown = new();         // 켜 둔 고정 연출(빛기둥 등). 끄는 건 부른 쪽(Hide)
    private readonly Dictionary<ECommonEffectType, EffectController> m_DicCommon = new();
    private readonly ChunkRenderer m_Chunk = new();

    private TelegraphController m_TelegraphCircle;
    private TelegraphController m_TelegraphLine;

    // ------------------------------------------------

    public override void Init()
    {
        m_DicCommon.Clear();
        foreach (ECommonEffectType type in System.Enum.GetValues(typeof(ECommonEffectType)))
        {
            m_DicCommon[type] = ResourceManager.Instance.Load<EffectController>($"{CommonPath.EffectDir}FX_{type}");
        }

        m_TelegraphCircle = ResourceManager.Instance.Load<TelegraphController>(CommonPath.TelegraphCircle);
        m_TelegraphLine = ResourceManager.Instance.Load<TelegraphController>(CommonPath.TelegraphLine);
    }

    public override void Tick(float deltaTime)
    {
        for (int i = m_ListActive.Count - 1; i >= 0; i--)
        {
            EffectController effect = m_ListActive[i];
            if (effect != null && effect.IsAlive)
                continue;

            m_ListActive.RemoveAt(i);
            ResourceManager.Instance.Despawn(effect);
        }

        m_Chunk.Tick(deltaTime);
    }

    public override void Clear()
    {
        foreach (EffectController effect in m_ListActive)
        {
            ResourceManager.Instance.Despawn(effect);
        }

        m_ListActive.Clear();

        foreach (EffectController effect in m_ListShown)
        {
            ResourceManager.Instance.Despawn(effect);
        }

        m_ListShown.Clear();
        m_Chunk.Clear();

        foreach (TelegraphController telegraph in m_ListTelegraph)
        {
            ResourceManager.Instance.Despawn(telegraph);
        }

        m_ListTelegraph.Clear();
    }

    // ---
    
    // 풀에서 꺼내 재생한다. 크기는 프리팹 기준 배율
    public EffectController Play(EffectController prefab, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (prefab == null)
            return null;

        EffectController effect = ResourceManager.Instance.Spawn(prefab, position, rotation);
        effect.transform.localScale = Vector3.one * scale;
        m_ListActive.Add(effect);
        
        return effect;
    }

    // 고정 연출 재생. 끝나면 알아서 돌려놓는다
    public EffectController Play(ECommonEffectType type, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        return Play(m_DicCommon.GetValueOrDefault(type), position, rotation, scale);
    }

    // 고정 연출을 켜 둔다. 끄는 건 부른 쪽이 Hide로
    public EffectController Show(ECommonEffectType type, Vector3 position, Quaternion rotation)
    {
        EffectController prefab = m_DicCommon.GetValueOrDefault(type);
        if (prefab == null)
            return null;

        EffectController effect = ResourceManager.Instance.Spawn(prefab, position, rotation);
        m_ListShown.Add(effect);
        return effect;
    }

    public void Hide(EffectController effect)
    {
        if (effect == null || m_ListShown.Remove(effect) == false)
            return;

        ResourceManager.Instance.Despawn(effect);
    }

    // 공격 예고를 꺼낸다. 모양·진행도는 받은 쪽이 매 프레임 넣고, 끝나면 HideTelegraph로 돌려준다
    public TelegraphController ShowTelegraph(bool isLine)
    {
        TelegraphController prefab = isLine ? m_TelegraphLine : m_TelegraphCircle;
        if (prefab == null)
            return null;

        TelegraphController telegraph = ResourceManager.Instance.Spawn(prefab, Vector3.zero, Quaternion.identity);
        m_ListTelegraph.Add(telegraph);
        
        return telegraph;
    }

    public void HideTelegraph(TelegraphController telegraph)
    {
        if (telegraph == null || m_ListTelegraph.Remove(telegraph) == false)
            return;

        ResourceManager.Instance.Despawn(telegraph);
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
}
