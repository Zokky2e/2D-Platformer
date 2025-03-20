using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockEffect", menuName = "Inventory/ItemEffects/BlockEffect")]
public class BlockEffect : ItemEffect<CharacterStats>
{
    public override string AdjustDescription(string description)
    {
        description = description + "\nThis item has block. \nBlock: Pressing right click causes you to block any incoming damage with your shield.";
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
        target.canUseBlock = true;
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.canUseBlock = false;
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
