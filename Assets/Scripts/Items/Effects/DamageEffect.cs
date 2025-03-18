using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Inventory/ItemEffects/DamageEffect")]
public class DamageEffect : ItemEffect<CharacterStats>
{
    public float damageBonus;

    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusDamage(damageBonus);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusDamage(-damageBonus);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
