using UnityEngine;

// ==================================================

// 공격·스킬 파티클 이펙트(AttackSpec.Effect). 판정이 처음 열릴 때 MeleeAttackFeature가 풀에서 꺼낸다.
// 꺼낼 때 처음부터 다시 재생하고, 자식 파티클까지 모두 끝나면 풀로 돌아간다.
// 파티클은 게임 시간으로 돌아 히트스톱 중에는 같이 멈춘다

[RequireComponent(typeof(ParticleSystem))]
public class AttackEffect : PoolableMono
{
    [field: SerializeField]
    public bool IsOnGround { get; private set; } = false; // 판정 높이 대신 주인 발밑에서 터진다(충격파·흙먼지)

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

    // virtual ---

    public override void OnPoolSpawn()
    {
        m_Particle.Clear(true);
        m_Particle.Play(true);
    }
}
