using UnityEngine;

// ==================================================

// 한 번 터지고 끝나는 파티클 이펙트 프리팹. 꺼내고 돌려놓는 일은 EffectService가 한다.
// 파티클은 게임 시간으로 돌아 히트스톱 중에는 같이 멈춘다

public class EffectController : PoolableMono
{
    [SerializeField] private ParticleSystem m_Particle = null; // 루트 파티클. 자식 파티클도 같이 재생된다

    [field: SerializeField]
    public bool IsOnGround { get; private set; } = false; // 부르는 쪽이 높이 대신 발밑에 놓는다(충격파·흙먼지)

    public bool IsAlive => m_Particle != null && m_Particle.IsAlive(true); // 자식 파티클까지 하나라도 살아 있는지

    // ------------------------------------------------

    // virtual ---

    public override void OnPoolSpawn()
    {
        if (m_Particle == null)
            return;

        m_Particle.Clear(true);
        m_Particle.Play(true);
    }
}
