using UnityEngine;

[CreateAssetMenu(fileName = "NewBleedDurationEffect", menuName = "Inventory/ItemEffects/BleedDurationEffect")]
public class BleedDurationEffect : ItemEffect<CharacterStats>
{
    public float bleedDuration;

    public override string AdjustDescription(string description)
    {
        description = description.Replace("{bleedDuration}", bleedDuration.ToString());
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
    }

    public override void RemoveEffect(CharacterStats target)
    {
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
