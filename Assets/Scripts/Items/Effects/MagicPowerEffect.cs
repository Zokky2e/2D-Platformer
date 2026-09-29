using UnityEngine;

[CreateAssetMenu(fileName = "NewMagicPowerEffect", menuName = "Inventory/ItemEffects/MagicPowerEffect")]
public class MagicPowerEffect : ItemEffect<CharacterStats>
{
    public float bonusMagicPower;

    public override string AdjustDescription(string description)
    {
        string sign = bonusMagicPower >= 0 ? "+" : "";
        description = description.Replace("{bonusMagicPower}", sign + bonusMagicPower.ToString());
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusMagicPower(bonusMagicPower);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusMagicPower(-bonusMagicPower);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
