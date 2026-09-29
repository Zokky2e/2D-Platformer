using Assets.Scripts;
using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header ("Health")]
    public float baseHealth = 100f;
    public float bonusHealth = 0f;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => baseHealth + bonusHealth; // Dynamic max HP

    public IEntity entity;

    [Header("IFrames")]
    public float iFramesDuration;
    public int numberOffFlashes;
    private SpriteRenderer spriteRend;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask enemyLayer;

    private int playerLayerNumber;
    private int enemyLayerNumber;
    private bool isInvulnerable = false;

    public void Awake()
    {
        CurrentHealth = MaxHealth;
        spriteRend = GetComponent<SpriteRenderer>();
        playerLayerNumber = (int)Math.Log(playerLayer.value, 2);
        enemyLayerNumber = (int)Math.Log(enemyLayer.value, 2);
    }

    // Returns true if the hit landed (not blocked, rolled through, fully absorbed by armor or during i-frames)
    public bool TakeDamage(float _damage)
    {
        if (CurrentHealth <= 0 || isInvulnerable)
            return false; // Already dead, or flashing after a hit
        if (entity.IsBlocking())
            return false;
        _damage = entity.TakeDamage(_damage);
        if (_damage == 0)
            return false;
        if (CurrentHealth - _damage > 0)
            StartCoroutine(Invunerability());
        ReduceHealth(_damage);
        return true;
    }

    // Damage over time (bleed, poison, burn): ignores armor, blocking and i-frames, no hurt animation
    public void TakeStatusDamage(float _damage)
    {
        if (CurrentHealth <= 0)
            return;
        ReduceHealth(_damage);
    }

    // Instant death (falling out of the level), regardless of blocking or i-frames
    public void Kill()
    {
        if (CurrentHealth > 0)
            ReduceHealth(CurrentHealth);
    }

    private void ReduceHealth(float _damage)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - _damage, 0, MaxHealth);
        if (CurrentHealth == 0)
        {
            entity.Die();
            OnDied();
        }
    }

    // Runs once when health reaches zero, whatever the damage source
    protected virtual void OnDied()
    {
    }

    public void SetHealth(float _health)
    {
        CurrentHealth = Mathf.Clamp(_health, 0, MaxHealth);
    }

    public void AddHealth(float _healthAmount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + _healthAmount, 0, MaxHealth);
    }

    private IEnumerator Invunerability()
    {
        if (playerLayerNumber == 6)
        {
            // Ignoring collisions alone doesn't stop enemy attacks, which call TakeDamage directly
            isInvulnerable = true;
            Physics2D.IgnoreLayerCollision(playerLayerNumber, enemyLayerNumber, true);
            for (int i = 0; i < numberOffFlashes; i++) 
            {
                spriteRend.color = new Color(1, 0, 0, 0.9f);
                yield return new WaitForSeconds(iFramesDuration / (numberOffFlashes * 2));
                spriteRend.color = Color.white;
                yield return new WaitForSeconds(iFramesDuration / (numberOffFlashes * 2));
            }
            Physics2D.IgnoreLayerCollision(playerLayerNumber, enemyLayerNumber, false);
            isInvulnerable = false;
        }

        yield break;
    }

}
