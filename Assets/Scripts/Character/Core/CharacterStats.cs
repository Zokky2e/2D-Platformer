using UnityEngine;

[System.Serializable]
public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseMoveSpeed = 4f;
    public float baseJumpHeight = 10f;
    public float baseDamage = 15f;
    public float baseArmor = 5f;
    public float baseMagicPower = 0f; // For future spells
    public float balancingArmorConstant = 50;
    public const float MoveSpeedPerAgility = 0.1f;

    [Header("Bonus Stats")]
    public float bonusMoveSpeed = 0f;
    public float bonusJumpHeight = 0f;
    public float bonusDamage = 0f;
    public float bonusArmor = 0f;
    public float bonusMagicPower = 0f;
    public float bonusAgility = 0f; // Each point adds MoveSpeedPerAgility to move speed

    [Header("On-hit Status Effects")] // Damage per second and duration in seconds, mostly from equipment
    public float bleedDamage = 0f;
    public float bleedDuration = 0f;
    public float poisonDamage = 0f;
    public float poisonDuration = 0f;
    public float burnDamage = 0f;
    public float burnDuration = 0f;

    [HideInInspector] public bool canUseBlock = false;


    public float TotalMoveSpeed => baseMoveSpeed + bonusMoveSpeed + bonusAgility * MoveSpeedPerAgility;
    public float TotalJumpHeight => baseJumpHeight + bonusJumpHeight;
    public float TotalDamage => baseDamage + bonusDamage;
    public float TotalArmor => baseArmor + bonusArmor;
    public float TotalMagicPower => baseMagicPower + bonusMagicPower;

    public void AddBonusMoveSpeed(float amount) => bonusMoveSpeed += amount;
    public void AddBonusJumpHeight(float amount) => bonusJumpHeight += amount;
    public void AddBonusDamage(float amount) => bonusDamage += amount;
    public void AddBonusArmor(float amount) => bonusArmor += amount;
    public void AddBonusMagicPower(float amount) => bonusMagicPower += amount;
    public void AddBonusAgility(float amount) => bonusAgility += amount;
    public void AddStatusEffectBonus(StatusEffectType type, bool isDuration, float amount)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
                if (isDuration) bleedDuration += amount; else bleedDamage += amount;
                break;
            case StatusEffectType.Poison:
                if (isDuration) poisonDuration += amount; else poisonDamage += amount;
                break;
            case StatusEffectType.Burn:
                if (isDuration) burnDuration += amount; else burnDamage += amount;
                break;
        }
    }

    // Applies this character's on-hit effects to something it just hit
    public void ApplyOnHitEffects(GameObject target)
    {
        StatusEffects.Apply(target, StatusEffectType.Bleed, bleedDamage, bleedDuration);
        StatusEffects.Apply(target, StatusEffectType.Poison, poisonDamage, poisonDuration);
        StatusEffects.Apply(target, StatusEffectType.Burn, burnDamage, burnDuration);
    }

    public float CalculateDamage(float _damage) => 
        Mathf.Floor(_damage * (1 - (TotalArmor / (TotalArmor + balancingArmorConstant))));
    public void ResetBonuses()
    {
        bonusMoveSpeed = 0f;
        bonusJumpHeight = 0f;
        bonusDamage = 0f;
        bonusArmor = 0f;
        bonusMagicPower = 0f;
        bonusAgility = 0f;
    }
}
