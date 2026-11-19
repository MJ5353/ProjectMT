using UnityEngine;

// ==================================================

// 한 번 터지고 끝나는 파티클 이펙트. Play로 풀에서 꺼내 처음부터 재생하고, 자식 파티클까지 모두 끝나면 풀로 돌아간다.
// 공격(AttackSpec.Effect) 외에도 위치·회전·크기만 주면 어디서든 쓴다.
// 파티클은 게임 시간으로 돌아 히트스톱 중에는 같이 멈춘다

[RequireComponent(typeof(ParticleSystem))]
public class EffectController : PoolableMono
{
    [field: SerializeField]
    public bool IsOnGround { get; private set; } = false; // 부르는 쪽이 높이 대신 발밑에 놓는다(충격파·흙먼지)

    private ParticleSystem m_Particle;

    // ------------------------------------------------

    private void Awake()
    {
        m_Particle = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (m_Particle.IsAlive(true))
            return;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.Despawn(this);
        else
            Destroy(gameObject);
    }

    // 프리팹이 없거나 풀이 없으면(전투 씬 밖) 아무것도 안 한다
    public static EffectController Play(EffectController prefab, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (prefab == null || ResourceManager.Instance == null)
            return null;

        EffectController effect = ResourceManager.Instance.Spawn(prefab, position, rotation);
        effect.transform.localScale = Vector3.one * scale;
        return effect;
    }

    // virtual ---

    public override void OnPoolSpawn()
    {
        m_Particle.Clear(true);
        m_Particle.Play(true);
    }
}
