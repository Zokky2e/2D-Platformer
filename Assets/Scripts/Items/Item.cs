using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Consumable,
    Weapon,
    Shield,
    Armor,
    Accessory
}

[System.Serializable]
public abstract class Item : ScriptableObject
{
    protected int _id;

    [SerializeField] protected string _name;

    [TextArea(3, 5)][SerializeField] protected string _description;

    [SerializeField] protected Sprite _sprite;

    [SerializeField] protected ItemType _type;

    [SerializeField] protected int _price;

    [SerializeField] protected bool _isSellable;

    public Sprite Sprite => _sprite;
    public int Id => _id;
    public string Name => _name;
    public string FixedDescription => _description;
    [HideInInspector] public string Description = "";
    public ItemType Type => _type;
    public int Price => _price;
    public bool IsSellable => _isSellable;

    // Explicitly specify the type argument for the generic ItemEffect<T>
    public List<ItemEffect<CharacterStats>> characterStatsEffects = new List<ItemEffect<CharacterStats>>();
    public List<ItemEffect<Health>> healthEffects = new List<ItemEffect<Health>>();
    public List<ItemEffect<CharacterStats>> onActivateCharacterStatsEffects = new List<ItemEffect<CharacterStats>>();
    public List<ItemEffect<Health>> onActivateHealthEffects = new List<ItemEffect<Health>>();

    public void AdjustDescription()
    {
        var newDescription = FixedDescription;
        foreach (var effect in characterStatsEffects)
        {
            newDescription = effect.AdjustDescription(newDescription);
        }

        foreach (var effect in healthEffects)
        {
            newDescription = effect.AdjustDescription(newDescription);
        }
        foreach (var effect in onActivateCharacterStatsEffects)
        {
            newDescription = effect.AdjustDescription(newDescription);
        }

        foreach (var effect in onActivateHealthEffects)
        {
            newDescription = effect.AdjustDescription(newDescription);
        }
        Description = newDescription;
    }

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
