using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Consumable,
    Weapon,
    Armor,
    Accessory
}

[System.Serializable]
public abstract class Item : ScriptableObject
{
    [SerializeField] protected string _name;

    [SerializeField] protected string _description;

    [SerializeField] protected Sprite _sprite;

    [SerializeField] protected ItemType _type;

    public Sprite Sprite => _sprite;
    public string Name => _name;
    public string Description => _description;
    public ItemType Type => _type;

    // Explicitly specify the type argument for the generic ItemEffect<T>
    public List<ItemEffect<CharacterStats>> characterStatsEffects = new List<ItemEffect<CharacterStats>>();
    public List<ItemEffect<Health>> healthEffects = new List<ItemEffect<Health>>();
    public List<ItemEffect<CharacterStats>> onActivateCharacterStatsEffects = new List<ItemEffect<CharacterStats>>();
    public List<ItemEffect<Health>> onActivateHealthEffects = new List<ItemEffect<Health>>();

    public void ApplyEffects(CharacterStats characterStats, Health health)
    {
        foreach (var effect in characterStatsEffects)
        {
            effect.ApplyEffect(characterStats);
        }

        foreach (var effect in healthEffects)
        {
            effect.ApplyEffect(health);
        }
    }

    public void RemoveEffects(CharacterStats characterStats, Health health)
    {
        foreach (var effect in characterStatsEffects)
        {
            effect.RemoveEffect(characterStats);
        }

        foreach (var effect in healthEffects)
        {
            effect.RemoveEffect(health);
        }
    }
    
    public virtual void UseItem(CharacterStats characterStats, Health health)
    {
        foreach (var effect in onActivateCharacterStatsEffects)
        {
            effect.ApplyEffect(characterStats);
        }

        foreach (var effect in onActivateHealthEffects)
        {
            effect.ApplyEffect(health);
        }
    }
}
