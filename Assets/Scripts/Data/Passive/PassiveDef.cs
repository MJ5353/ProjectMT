using UnityEngine;

// ==================================================

// 수치 하나로 안 되는 효과(정비 스페셜 등). 정해진 지점(훅)만 열고, 유닛 쪽 PassiveFeature가 차례로 부른다.
// 효과마다 하위 클래스 하나·에셋 하나. 노드·장비 등은 에셋을 참조만 하고 PassiveFeature.AddSource로 넣는다.
// 훅: 공격이 나갈 때(피해·범위) · 피해를 받을 때 · 맞은 뒤 · 주웠을 때. 유닛 밖 일(골드 등)은 땅에 떨구는 식으로 전투 흐름에 맡긴다
public abstract class PassiveDef : DataDef
{
    // 공격이 나갈 때. 피해·판정 범위 배율에 곱한다(DamageCalculator)
    public virtual void ModifyAttack(UnitController owner, AttackSpec spec, ref float damageRate, ref float radiusRate) { }

    // 피해를 받을 때. 받는 피해 배율에 곱한다(DamageCalculator)
    public virtual void ModifyTaken(UnitController owner, ref float takenRate) { }

    // 맞은 뒤(체력이 깎이고 반응까지 끝난 뒤)
    public virtual void OnHit(UnitController owner, in HitParams hit) { }

    // 버섯·재화를 주웠을 때(BattleService)
    public virtual void OnPickup(UnitController owner, in LootPicked loot) { }
}
