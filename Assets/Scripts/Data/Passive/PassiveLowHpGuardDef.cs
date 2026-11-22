using UnityEngine;

// ==================================================

// 체력이 낮을 때 받는 피해 감소(든든한 뿌리)
[CreateAssetMenu(menuName = "MushTaker/Data/Passive/LowHpGuard", fileName = "Passive_")]
public class PassiveLowHpGuardDef : PassiveDef
{
    [field: SerializeField]
    public float HpRatio { get; private set; } = 0.3f;      // 이 체력 비율 이하일 때

    [field: SerializeField]
    public float ReduceRate { get; private set; } = 0.3f;   // 받는 피해 감소(0.3 = -30%)

    // ------------------------------------------------

    public override void ModifyTaken(UnitController owner, ref float takenRate)
    {
        if (owner.HealthF.Ratio <= HpRatio)
            takenRate *= 1f - ReduceRate;
    }

#if UNITY_EDITOR
    public void EditorSetup(float hpRatio, float reduceRate)
    {
        HpRatio = hpRatio;
        ReduceRate = reduceRate;
    }
#endif
}
