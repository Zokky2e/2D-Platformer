using UnityEngine;

[CreateAssetMenu(fileName = "NewHealthEffect", menuName = "Inventory/ItemEffects/HealthEffect")]
public class HealthEffect : ItemEffect<Health>
{
    public float bonusHealth;

    public override void ApplyEffect(Health target)
    {
        target.bonusHealth += bonusHealth;
    }

    public override void RemoveEffect(Health target)
    {
        target.bonusHealth -= bonusHealth;
    }

    public override void UseItem(Health item)
    {
        return;
    }
}
