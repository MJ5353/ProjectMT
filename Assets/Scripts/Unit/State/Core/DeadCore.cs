using UnityEngine;

// ==================================================

// 사망. 멈춰서 Death 애니를 튼다. 끝나지 않고 무엇에도 끊기지 않는다(부활은 StateFeature.Revive).
// AI 유닛(IsDespawn)은 발밑 먼지를 내고, 넉백으로 밀려난 뒤 충돌을 끄고, 잠시 뒤 사라진다. 이 시간은 Think(판단 시간)로 재서 멈춘 동안은 사라지지 않는다

public class DeadCore : CoreState
{
    private const float CollisionOffTime = 0.4f;
    private const float DespawnTime = 2f;
    
    public bool IsDespawn { get; set; }     // AI 유닛만(StateFeature.EnableDespawnOnDead)

    private float m_ThinkTime;
    private bool m_IsCollisionOff;

    public override ECoreState Type => ECoreState.Dead;

    // ------------------------------------------------

    public DeadCore(UnitController owner) : base(owner) { }

    public override bool CanInterruptBy(ECoreState next)
    {
        return false;
    }

    protected override void Enter()
    {
        Owner.MoveF.Stop();
        Owner.AnimF.Play(AnimKey.Death, true);

        if (IsDespawn == false)
            return;

        m_ThinkTime = 0f;
        m_IsCollisionOff = false;

        Owner.AnimF.SetSpeed(1f);
        PlayDeathEffect();
    }

    public override void Think(float deltaTime)
    {
        if (IsDespawn == false)
            return;

        m_ThinkTime += deltaTime;

        if (m_IsCollisionOff == false && m_ThinkTime >= CollisionOffTime)
        {
            m_IsCollisionOff = true;
            Owner.MoveF.SetCollision(false);
        }

        if (m_ThinkTime >= DespawnTime)
            Owner.Despawn();
    }

    // ==================================================

    // 발밑에 먼지. 몸 반지름 0.5를 기준 크기로 키운다
    private void PlayDeathEffect()
    {
        EffectService effectS = FlowManager.Instance.EffectS;
        if (effectS == null)
            return;

        float scale = Owner.HurtBody.radius * 2f;
        effectS.Play(ECommonEffectType.Death, Owner.transform.position, Quaternion.identity, scale);
    }
}
