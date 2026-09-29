using UnityEngine;

// Equipped: raises the wearer's recharging shield ("Shield" in healthEffects).
// Used (consumables): grants a temporary shield for duration seconds ("TemporaryShield" in onActivateHealthEffects).
[CreateAssetMenu(fileName = "NewShieldEffect", menuName = "Inventory/ItemEffects/ShieldEffect")]
public class ShieldEffect : ItemEffect<Health>
{
    public float bonusShield;
    public bool temporary;
    public float duration; // Temporary only; 0 lasts until used up

    public override string AdjustDescription(string description)
    {
        string sign = bonusShield >= 0 ? "+" : "";
        description = description.Replace("{bonusShield}", sign + bonusShield.ToString());
        return description.Replace("{shieldDuration}", duration.ToString());
    }

    public override void ApplyEffect(Health target)
    {
        if (temporary)
            target.AddTemporaryShield(bonusShield, duration);
        else
            target.bonusShield += bonusShield;
    }

    public override void RemoveEffect(Health target)
    {
        if (!temporary)
            target.bonusShield -= bonusShield;
    }

    public override void UseItem(Health item)
    {
        return;
    }
}
