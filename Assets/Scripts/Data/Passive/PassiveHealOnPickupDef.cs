using UnityEngine;

// ==================================================

// 버섯을 주울 때 체력 회복(포자 재생). 재화(골드)를 주울 때는 회복하지 않는다
[CreateAssetMenu(menuName = "MushTaker/Data/Passive/HealOnPickup", fileName = "Passive_")]
public class PassiveHealOnPickupDef : PassiveDef
{
    [field: SerializeField]
    public int HealPerMushroom { get; private set; } = 3;

    // ------------------------------------------------

    public override void OnPickup(UnitController owner, in LootPicked loot)
    {
        if (GameDatabase.S_TryGet(loot.ItemID, out MushroomDef _) == false)
            return;

        owner.HealthF.Heal(HealPerMushroom * loot.Amount);
    }

#if UNITY_EDITOR
    public void EditorSetup(int healPerMushroom)
    {
        HealPerMushroom = healPerMushroom;
    }
#endif
}
