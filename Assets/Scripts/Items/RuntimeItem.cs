using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuntimeItem : Item
{
    public void SetData(ItemData data)
    {
        List<Sprite> allSprites = new();
        _id = data.id;
        _name = data.name;
        _description = data.description;
        _type = data.type;
        if (data.spriteName.Contains("armor"))
        {
            allSprites = Resources.LoadAll<Sprite>("Sprites/basic_armor").ToList();
        }
        else if(data.spriteName.Contains("clothing"))
        {
            allSprites = Resources.LoadAll<Sprite>("Sprites/basic_clothing").ToList();
        }

        if (allSprites.Count > 0) 
        {
            foreach (Sprite sprite in allSprites) 
            {
                if (sprite.name == data.spriteName)
                {
                    _sprite = sprite;
                }
            }
        }
        else
        {
            _sprite = Resources.Load<Sprite>($"Sprites/{data.spriteName}");
        }

        characterStatsEffects = ConvertCharacterStatsEffects(data.characterStatsEffects);
        healthEffects = ConvertHealthEffects(data.healthEffects);
        onActivateCharacterStatsEffects = ConvertCharacterStatsEffects(data.onActivateCharacterStatsEffects);
        onActivateHealthEffects = ConvertHealthEffects(data.onActivateHealthEffects);
    }

    private List<ItemEffect<CharacterStats>> ConvertCharacterStatsEffects(List<EffectData> effectDataList)
    {
        var list = new List<ItemEffect<CharacterStats>>();
        foreach (var data in effectDataList)
        {
            switch (data.effectType)
            {
                case "Armor":
                    var armorEffect = ScriptableObject.CreateInstance<ArmorEffect>();
                    armorEffect.bonusArmor = data.value;
                    list.Add(armorEffect);
                    break;
                case "Damage":
                    var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                    damageEffect.bonusDamage = data.value;
                    list.Add(damageEffect);
                    break;
                case "BleedDamage":
                    var bleedDamage = ScriptableObject.CreateInstance<BleedDamageEffect>();
                    bleedDamage.bleedDamage = data.value;
                    list.Add(bleedDamage);
                    break;
                case "BleedDuration":
                    var bleedDuration = ScriptableObject.CreateInstance<BleedDurationEffect>();
                    bleedDuration.bleedDuration = data.value;
                    list.Add(bleedDuration);
                    break;
                    // Add more CharacterStats-based effects here
            }
        }
        return list;
    }

    private List<ItemEffect<Health>> ConvertHealthEffects(List<EffectData> effectDataList)
    {
        var list = new List<ItemEffect<Health>>();
        foreach (var data in effectDataList)
        {
            switch (data.effectType)
            {
                case "Health":
                    var effect = ScriptableObject.CreateInstance<HealthEffect>();
                    effect.bonusHealth = data.value;
                    list.Add(effect);
                    break;

                    // Add more Health-based effects here
            }
        }
        return list;
    }
}
