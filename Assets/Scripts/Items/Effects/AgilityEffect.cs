using UnityEngine;

// Each point of agility adds CharacterStats.MoveSpeedPerAgility to move speed
[CreateAssetMenu(fileName = "NewAgilityEffect", menuName = "Inventory/ItemEffects/AgilityEffect")]
public class AgilityEffect : ItemEffect<CharacterStats>
{
    public float bonusAgility;

    public override string AdjustDescription(string description)
    {
        string sign = bonusAgility >= 0 ? "+" : "";
        description = description.Replace("{bonusAgility}", sign + bonusAgility.ToString());
        return description;
    }
    public override void ApplyEffect(CharacterStats target)
    {
        target.AddBonusAgility(bonusAgility);
    }

    public override void RemoveEffect(CharacterStats target)
    {
        target.AddBonusAgility(-bonusAgility);
    }
    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
