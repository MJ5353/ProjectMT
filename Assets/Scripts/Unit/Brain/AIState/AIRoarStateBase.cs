using UnityEngine;

// ==================================================

// 포효(Boss: 첫 발견·분노). 멈춰서 대상을 보며 Roar 애니를 한 번 틀고 추적으로 간다.
// 시작할 때 카메라를 흔들고 BossRoared를 알린다(HUD가 붉은 가장자리·이름 배너)

public class AIRoarStateBase : AIStateBase
{
    private const string RoarClip = "Roar";
    private float m_Length;

    // ------------------------------------------------

    public AIRoarStateBase(AIBrain ai) : base(ai) { }

    public override void Enter()
    {
        m_Controller.UnitExecutor.SetMove(Vector3.zero, 0f);
        m_Length = m_Controller.AnimF.GetLength(RoarClip);
        m_Controller.AnimF.Play(RoarClip, true);

        DefineDef define = GameManager.Instance.Define;
        CameraManager.Instance.Shake(define.Camera.RoarShake, define.Camera.RoarShakeTime);
        SEventManager.Publish(new BossRoared(m_Controller, m_AI.IsEnraged));
    }

    public override void Update(float deltaTime)
    {
        m_Controller.MoveF.FaceTo(TargetSelector.GetDirection(), deltaTime);
        if (TimeInState >= m_Length)
            m_AI.Change(EAIState.Chase);
    }
}
