using UnityEngine;

public enum ManaStat
{
    MaxMana, // "Mana", {bonusMana}
    ManaRegen, // "ManaRegen", {bonusManaRegen}, per second
    Restore // "RestoreMana" on consumables, {restoreMana}
}

// Changes the wearer's Mana component; characters without one are unaffected
[CreateAssetMenu(fileName = "NewManaEffect", menuName = "Inventory/ItemEffects/ManaEffect")]
public class ManaEffect : ItemEffect<CharacterStats>
{
    public ManaStat stat;
    public float value;

    public override string AdjustDescription(string description)
    {
        string placeholder = stat switch
        {
            ManaStat.MaxMana => "{bonusMana}",
            ManaStat.ManaRegen => "{bonusManaRegen}",
            _ => "{restoreMana}",
        };
        string sign = value >= 0 ? "+" : "";
        return description.Replace(placeholder, sign + value.ToString());
    }

    public override void ApplyEffect(CharacterStats target)
    {
        if (!target.TryGetComponent(out Mana mana))
            return;
        switch (stat)
        {
            case ManaStat.MaxMana:
                mana.bonusMana += value;
                break;
            case ManaStat.ManaRegen:
                mana.bonusManaRegen += value;
                break;
            case ManaStat.Restore:
                mana.Restore(value);
                break;
        }
    }

    public override void RemoveEffect(CharacterStats target)
    {
        if (!target.TryGetComponent(out Mana mana))
            return;
        if (stat == ManaStat.MaxMana)
            mana.bonusMana -= value;
        else if (stat == ManaStat.ManaRegen)
            mana.bonusManaRegen -= value;
    }

    public override void UseItem(CharacterStats item)
    {
        return;
    }
}
