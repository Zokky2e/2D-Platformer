using UnityEngine;

// Gives the wearer's hits a damage-over-time effect (bleed, poison or burn). Each item effect adds either the
// damage per second or the duration in seconds; values from several items add up.
// items.json effect types: BleedDamage, BleedDuration, PoisonDamage, PoisonDuration, BurnDamage, BurnDuration.
[CreateAssetMenu(fileName = "NewOnHitStatusEffect", menuName = "Inventory/ItemEffects/OnHitStatusEffect")]
public class OnHitStatusEffect : ItemEffect<CharacterStats>
{
    public StatusEffectType statusType;
    public bool isDuration; // false: damage per second, true: duration in seconds
    public float value;

    // e.g. {bleedDamage}, {poisonDuration}
    private string Placeholder => "{" + statusType.ToString().ToLowerInvariant() + (isDuration ? "Duration" : "Damage") + "}";

    public override string AdjustDescription(string description)
    {
        string sign = !isDuration && value >= 0 ? "+" : "";
        return description.Replace(Placeholder, sign + value.ToString());
    }

    public override void ApplyEffect(CharacterStats target)
    {
        target.AddStatusEffectBonus(statusType, isDuration, value);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddStatusEffectBonus(statusType, isDuration, -value);
    }

    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
