using UnityEngine;

// ==================================================

// AI 대상 감지(유닛 수명). 대상이 없으면 시야(AIDef.SightRange) 안 가장 가까운 적을 잡고, 맞으면 때린 쪽을 노린다. 스폰 자리(배회 기준)도 든다.
// Enter(꺼낼 때·다시 켤 때)에 처음 값으로 돌린다. 다시 켜는 건 스폰 직후 멈춰 둔 시작 팝업뿐이라 스폰 때와 같다. 펫은 쓰지 않는다

public class TargetSenseSub : SubState
{
    private readonly AIDef m_Def;

    public override ESubLifetime Lifetime => ESubLifetime.Unit;
    public TargetSelector Selector { get; }         // 노리는 적·거리·방향. AI 코어가 읽는다
    public Vector3 HomePosition { get; private set; } // 스폰 자리. 배회 기준

    // ------------------------------------------------

    public TargetSenseSub(UnitController owner, AIDef def) : base(owner)
    {
        m_Def = def;
        Selector = new TargetSelector(owner);
    }

    protected override void Enter()
    {
        Selector.Reset();
        HomePosition = Owner.transform.position;
    }

    protected override void Tick(float deltaTime)
    {
        Selector.Refresh(m_Def.SightRange);
    }

    // 풀로 돌아갈 때·멈출 때. 풀에 든 유닛을 붙잡고 있지 않게 비운다
    public override void Exit()
    {
        Selector.Clear();
    }

    public override void OnHit(in HitParams hit)
    {
        Selector.SetTarget(hit.Attacker);
    }
}
