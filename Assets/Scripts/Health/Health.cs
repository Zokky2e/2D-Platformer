using Assets.Scripts;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float startingHealth;
    public float currentHealth { get; private set; }

    public IEntity entity;

    public void Awake()
    {
        currentHealth = startingHealth;
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);
        if (currentHealth > 0)
        {
            entity.TakeDamage();
        }
        else
        {
            entity.Die();
        }
    }

    public void AddHealth(float _healthAmount)
    {
        currentHealth = Mathf.Clamp(currentHealth + _healthAmount, 0, startingHealth);
    }

}
