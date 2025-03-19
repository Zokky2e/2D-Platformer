using UnityEngine;

[CreateAssetMenu(fileName = "NewArmorEffect", menuName = "Inventory/ItemEffects/ArmorEffect")]
public class ArmorEffect : ItemEffect<CharacterStats>
{
    public float bonusArmor;

    public override string AdjustDescription(string description)
    {
        string sign = bonusArmor >= 0 ? "+" : "";
        description = description.Replace("{bonusArmor}", sign + bonusArmor.ToString());
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusArmor(bonusArmor);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusArmor(-bonusArmor);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
