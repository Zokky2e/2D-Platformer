using UnityEngine;

[CreateAssetMenu(fileName = "NewBleedDamageEffect", menuName = "Inventory/ItemEffects/BleedDamageEffect")]
public class BleedDamageEffect : ItemEffect<CharacterStats>
{
    public float bleedDamage;

    public override string AdjustDescription(string description)
    {
        string sign = bleedDamage >= 0 ? "+" : "";
        description = description.Replace("{bleedDamage}", sign + bleedDamage.ToString());
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusDamage(bleedDamage);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusDamage(-bleedDamage);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
