using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Inventory/ItemEffects/DamageEffect")]
public class DamageEffect : ItemEffect<CharacterStats>
{
    public float bonusDamage;

    public override string AdjustDescription(string description)
    {
        string sign = bonusDamage >= 0 ? "+" : "";
        description = description.Replace("{bonusDamage}", sign + bonusDamage.ToString());
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusDamage(bonusDamage);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusDamage(-bonusDamage);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
