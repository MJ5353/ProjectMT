using UnityEngine;

// ==================================================

// 플레이어 기본 코어(자유 이동). 이동·대기는 MoveCore 그대로이고, 입력(PlayerInputSub)을 속도·걷기/달리기로 바꾸는 것만 더한다. 수치는 PlayerDef

public class FreeCore : MoveCore
{
    private const float DeadZone = 0.1f;   // 입력 세기가 이보다 작으면 멈춰서 Idle

    private readonly PlayerDef m_Def;

    public override ECoreState Type => ECoreState.Free;

    // ------------------------------------------------

    public FreeCore(UnitController owner, PlayerDef def) : base(owner)
    {
        m_Def = def;
    }

    // input: 카메라 기준 수평 입력(세기 0~1). 세기가 RunThreshold 이상이면 Run 애니
    public void SetInput(Vector3 input)
    {
        float strength = Mathf.Clamp01(input.magnitude);
        float speed = strength < DeadZone ? 0f : Owner.StatF.Get(EStatType.MoveSpeed, m_Def.MoveSpeed) * strength;

        input.y = 0f;
        SetMove(input.normalized * speed, strength >= m_Def.RunThreshold);
    }
}
