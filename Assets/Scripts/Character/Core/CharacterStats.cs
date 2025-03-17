using UnityEngine;

[System.Serializable]
public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseMoveSpeed = 4f;
    public float baseJumpHeight = 10f;
    public float baseDamage = 15f;
    public float baseArmor = 5f;
    public float balancingArmorConstant = 50;

    [Header("Bonus Stats")]
    public float bonusMoveSpeed = 0f;
    public float bonusJumpHeight = 0f;
    public float bonusDamage = 0f;
    public float bonusArmor = 0f;

    public float TotalMoveSpeed => baseMoveSpeed + bonusMoveSpeed;
    public float TotalJumpHeight => baseJumpHeight + bonusJumpHeight;
    public float TotalDamage => baseDamage + bonusDamage;
    public float TotalArmor => baseArmor + bonusArmor;

    public void AddBonusMoveSpeed(float amount) => bonusMoveSpeed += amount;
    public void AddBonusJumpHeight(float amount) => bonusJumpHeight += amount;
    public void AddBonusDamage(float amount) => bonusDamage += amount;
    public void AddBonusArmor(float amount) => bonusArmor += amount;
    public float CalculateDamage(float _damage) => 
        Mathf.Floor(_damage * (1 - (TotalArmor / (TotalArmor + balancingArmorConstant))));
    public void ResetBonuses()
    {
        bonusMoveSpeed = 0f;
        bonusJumpHeight = 0f;
        bonusDamage = 0f;
        bonusArmor = 0f;
    }
}
