using UnityEngine;

[CreateAssetMenu(fileName = "NewHealEffect", menuName = "Inventory/ItemEffects/HealEffect")]
public class HealEffect : ItemEffect<Health>
{
    public float healAmount;

    public override void ApplyEffect(Health target)
    {
        target.AddHealth(healAmount);
    }

    public override void RemoveEffect(Health target)
    {
        target.AddHealth(-healAmount);
    }
    public override void UseItem(Health item)
    {
        return;
    }
}