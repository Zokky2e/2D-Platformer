using System.Collections.Generic;
using UnityEngine;

public class RuntimeItem : Item
{
    public void SetData(ItemData data)
    {
        _name = data.name;
        _description = data.description;
        _type = data.type;
        _sprite = Resources.Load<Sprite>($"Sprites/{data.spriteName}");

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
                    var effect = ScriptableObject.CreateInstance<ArmorEffect>();
                    effect.bonusArmor = data.value;
                    list.Add(effect);
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
