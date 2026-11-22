using UnityEngine;

// ==================================================

public class AIDeadStateBase : AIStateBase
{
    private const float CollisionOffTime = 0.4f;
    private const float DestroyTime = 2f;

    private bool m_IsCollisionOff;

    // ------------------------------------------------

    public AIDeadStateBase(AIBrain ai) : base(ai) { }

    public override void Enter()
    {
        m_IsCollisionOff = false;
        m_Controller.AnimF.SetSpeed(1f); // 멈춤·Death 애니는 사망 행동(DeadAction)이 한다

        PlayDeathEffect();
    }

    public override void Update(float deltaTime)
    {
        // 넉백으로 밀려난 뒤 충돌을 끈다
        if (m_IsCollisionOff == false && TimeInState >= CollisionOffTime)
        {
            m_IsCollisionOff = true;
            m_Controller.MoveF.SetCollision(false);
        }

        if (TimeInState >= DestroyTime)
            m_Controller.Despawn();
    }

    // 발밑에 먼지. 몸 반지름 0.5를 기준 크기로 키운다
    private void PlayDeathEffect()
    {
        EffectService effectS = FlowManager.Instance.EffectS;
        if (effectS == null)
            return;
        
        float radius = m_Controller.HurtBody.E_Alive()?.radius ?? 0.5f;
        float scale = radius * 2f; // 몸이 없으면 1
        effectS.Play(ECommonEffectType.Death, m_Controller.transform.position, Quaternion.identity, scale);
    }
}
