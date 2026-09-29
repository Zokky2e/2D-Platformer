using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuntimeItem : Item
{
    public void SetData(ItemData data)
    {
        _id = data.id;
        _name = data.name;
        _description = data.description;
        _type = data.type;
        _price = data.price;
        _isSellable = data.isSellable;
        SetSprite(data);

        characterStatsEffects = ConvertCharacterStatsEffects(data.characterStatsEffects);
        healthEffects = ConvertHealthEffects(data.healthEffects);
        onActivateCharacterStatsEffects = ConvertCharacterStatsEffects(data.onActivateCharacterStatsEffects);
        onActivateHealthEffects = ConvertHealthEffects(data.onActivateHealthEffects);
    }

    private void SetSprite(ItemData data)
    {
        // A standalone sprite in Resources/Sprites (e.g. weapon_elven_bow), or a sub-sprite "<sheet>_<n>" of a
        // sprite sheet there (e.g. basic_clothing_10 in basic_clothing)
        _sprite = Resources.Load<Sprite>($"Sprites/{data.spriteName}");
        int split = data.spriteName.LastIndexOf('_');
        if (_sprite == null && split > 0)
        {
            string sheet = data.spriteName.Substring(0, split);
            _sprite = Resources.LoadAll<Sprite>($"Sprites/{sheet}").FirstOrDefault(sprite => sprite.name == data.spriteName);
        }
        if (_sprite == null)
            Debug.LogWarning($"Item '{data.name}' has no sprite '{data.spriteName}' in Resources/Sprites");
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
                case "Block":
                    var blockEffect = ScriptableObject.CreateInstance<BlockEffect>();
                    list.Add(blockEffect);
                    break;
                case "Damage":
                    var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                    damageEffect.bonusDamage = data.value;
                    list.Add(damageEffect);
                    break;
                case "BleedDamage":
                    list.Add(CreateOnHitStatusEffect(StatusEffectType.Bleed, false, data.value));
                    break;
                case "BleedDuration":
                    list.Add(CreateOnHitStatusEffect(StatusEffectType.Bleed, true, data.value));
                    break;
                case "PoisonDamage":
                    list.Add(CreateOnHitStatusEffect(StatusEffectType.Poison, false, data.value));
                    break;
                case "PoisonDuration":
                    list.Add(CreateOnHitStatusEffect(StatusEffectType.Poison, true, data.value));
                    break;
                case "BurnDamage":
                    list.Add(CreateOnHitStatusEffect(StatusEffectType.Burn, false, data.value));
                    break;
                case "BurnDuration":
                    list.Add(CreateOnHitStatusEffect(StatusEffectType.Burn, true, data.value));
                    break;
                case "MagicPower":
                    var magicPower = ScriptableObject.CreateInstance<MagicPowerEffect>();
                    magicPower.bonusMagicPower = data.value;
                    list.Add(magicPower);
                    break;
                case "Agility":
                    var agility = ScriptableObject.CreateInstance<AgilityEffect>();
                    agility.bonusAgility = data.value;
                    list.Add(agility);
                    break;
                case "Mana":
                    list.Add(CreateManaEffect(ManaStat.MaxMana, data.value));
                    break;
                case "ManaRegen":
                    list.Add(CreateManaEffect(ManaStat.ManaRegen, data.value));
                    break;
                case "RestoreMana":
                    list.Add(CreateManaEffect(ManaStat.Restore, data.value));
                    break;
                    // Add more CharacterStats-based effects here
            }
        }
        return list;
    }

    private static OnHitStatusEffect CreateOnHitStatusEffect(StatusEffectType type, bool isDuration, float value)
    {
        var effect = ScriptableObject.CreateInstance<OnHitStatusEffect>();
        effect.statusType = type;
        effect.isDuration = isDuration;
        effect.value = value;
        return effect;
    }

    private static ManaEffect CreateManaEffect(ManaStat stat, float value)
    {
        var effect = ScriptableObject.CreateInstance<ManaEffect>();
        effect.stat = stat;
        effect.value = value;
        return effect;
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
                case "Heal":
                    var healEffect = ScriptableObject.CreateInstance<HealEffect>();
                    healEffect.healAmount = data.value;
                    list.Add(healEffect);
                    break;

                case "Shield":
                case "TemporaryShield":
                    var shield = ScriptableObject.CreateInstance<ShieldEffect>();
                    shield.bonusShield = data.value;
                    shield.temporary = data.effectType == "TemporaryShield";
                    shield.duration = data.duration;
                    list.Add(shield);
                    break;
                    // Add more Health-based effects here
            }
        }
        return list;
    }
}
