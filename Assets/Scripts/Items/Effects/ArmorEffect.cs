using UnityEngine;

[CreateAssetMenu(fileName = "NewArmorEffect", menuName = "Inventory/ItemEffects/ArmorEffect")]
public class ArmorEffect : ItemEffect<CharacterStats>
{
    public float armorBonus;

    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusArmor(armorBonus);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusArmor(-armorBonus);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
