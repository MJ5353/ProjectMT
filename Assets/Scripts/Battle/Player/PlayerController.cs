
// ==================================================

// 채집꾼 조립 허브. 몸(UnitController)에 대시를 켜고 판단은 입력(PlayerInputFeature)으로 둔다. 수치는 PlayerDef

public class PlayerController : UnitController<PlayerDef>
{
    public override ETeam Team => ETeam.Player;

    // ------------------------------------------------

    protected override void SetFeatures()
    {
        Action.EnableDash(Def.DashSpeed, Def.DashTime, Def.DashCooldown, Def.IsDashInvincible, Def.DashAnimSpeed);
        Action.Attack.CancelDelay = Def.CancelDelay;
    }

    protected override BrainFeature CreateBrain()
    {
        return new PlayerInputFeature(this, Def);
    }

    // 몬스터가 노리거나 버섯이 빨려 들 수 있는 상태인지. 판단 기준은 여기서만 정한다
    public bool IsValid()
    {
        return IsDead == false;
    }
}
