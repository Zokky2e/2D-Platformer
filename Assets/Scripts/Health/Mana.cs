using UnityEngine;

// Mana pool for a future spell system: max mana from base plus equipment, regenerating over time.
// Nothing spends mana yet; spells should call TrySpend and scale with CharacterStats.TotalMagicPower.
public class Mana : MonoBehaviour
{
    public float baseMana = 50f;
    public float bonusMana = 0f;
    public float baseManaRegen = 1f; // Per second
    public float bonusManaRegen = 0f;

    public float MaxMana => baseMana + bonusMana;
    public float ManaRegen => baseManaRegen + bonusManaRegen;
    public float CurrentMana { get; private set; }

    private void Awake()
    {
        CurrentMana = MaxMana;
    }

    private void Update()
    {
        // Also clamps when unequipping lowers the max
        CurrentMana = Mathf.Clamp(CurrentMana + ManaRegen * Time.deltaTime, 0, MaxMana);
    }

    public bool TrySpend(float amount)
    {
        if (CurrentMana < amount)
            return false;
        CurrentMana -= amount;
        return true;
    }

    public void Restore(float amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana + amount, 0, MaxMana);
    }

    public void SetMana(float value)
    {
        CurrentMana = Mathf.Clamp(value, 0, MaxMana);
    }
}
