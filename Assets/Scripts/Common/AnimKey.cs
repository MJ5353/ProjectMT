
// ==================================================

// 행동이 트는 공통 애니 상태 이름. 애니메이터 상태 이름 = 클립 이름(BattleSetup 메뉴가 만든다).
// 공격 클립은 SkillDef 이름(Attack_NN·Skill_NN)을 AttackSpec.Clip으로 따로 쓴다

public static class AnimKey
{
    public const string Idle = "Idle";
    public const string Walk = "Walk";
    public const string Run = "Run";
    public const string Hit = "Hit";
    public const string Death = "Death";
}
