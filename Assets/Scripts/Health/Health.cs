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
    public event Action Died; // Raised once when health reaches zero

    [Header("Shield")] // Absorbs damage before health
    public float baseShield = 0f;
    public float bonusShield = 0f; // From equipment
    public float shieldRechargeDelay = 3f; // Seconds without taking damage before the shield refills
    public float shieldRechargeRate = 10f; // Points per second
    public float MaxShield => baseShield + bonusShield;
    public float CurrentShield { get; private set; } // Recharging barrier, up to MaxShield
    public float TemporaryShield { get; private set; } // From consumables; doesn't recharge, can expire
    public float TotalShield => CurrentShield + TemporaryShield;
    private float lastDamageTime = float.NegativeInfinity;
    private Coroutine temporaryShieldExpiry;

    [Header("IFrames")]
    public float iFramesDuration;
    public int numberOffFlashes;
    private SpriteRenderer spriteRend;
    public Color BaseColor { get; private set; } = Color.white; // Sprite color before any flash or tint effect
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask enemyLayer;

    private int playerLayerNumber;
    private int enemyLayerNumber;
    private bool isInvulnerable = false;

    public void Awake()
    {
        CurrentHealth = MaxHealth;
        CurrentShield = MaxShield;
        spriteRend = GetComponent<SpriteRenderer>();
        if (spriteRend != null)
            BaseColor = spriteRend.color; // Recorded at spawn, before anything can be flashing
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
        _damage = AbsorbWithShield(_damage);
        if (_damage <= 0)
            return false; // Soaked up by the shield
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
        _damage = AbsorbWithShield(_damage);
        if (_damage > 0)
            ReduceHealth(_damage);
    }

    // Instant death (falling out of the level), regardless of blocking or i-frames
    public void Kill()
    {
        if (CurrentHealth > 0)
            ReduceHealth(CurrentHealth);
    }

    // Shield points soak damage first (temporary before recharging); returns what gets through to health
    private float AbsorbWithShield(float _damage)
    {
        lastDamageTime = Time.time;
        float fromTemporary = Mathf.Min(TemporaryShield, _damage);
        TemporaryShield -= fromTemporary;
        _damage -= fromTemporary;
        float fromShield = Mathf.Min(CurrentShield, _damage);
        CurrentShield -= fromShield;
        return _damage - fromShield;
    }

    // Consumable barrier on top of the regular shield. Replaces a weaker one and restarts its timer;
    // a duration of 0 or less lasts until it's used up
    public void AddTemporaryShield(float amount, float duration)
    {
        TemporaryShield = Mathf.Max(TemporaryShield, amount);
        if (temporaryShieldExpiry != null)
            StopCoroutine(temporaryShieldExpiry);
        temporaryShieldExpiry = duration > 0 ? StartCoroutine(ExpireTemporaryShield(duration)) : null;
    }

    private IEnumerator ExpireTemporaryShield(float duration)
    {
        yield return new WaitForSeconds(duration);
        TemporaryShield = 0;
        temporaryShieldExpiry = null;
    }

    private void Update()
    {
        // Unequipping a shield item lowers the cap; otherwise refill once no damage came in for a while
        if (CurrentShield > MaxShield)
            CurrentShield = MaxShield;
        else if (CurrentShield < MaxShield && CurrentHealth > 0 && Time.time - lastDamageTime >= shieldRechargeDelay)
            CurrentShield = Mathf.Min(MaxShield, CurrentShield + shieldRechargeRate * Time.deltaTime);
    }

    private void ReduceHealth(float _damage)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - _damage, 0, MaxHealth);
        if (CurrentHealth == 0)
        {
            entity.Die();
            OnDied();
            Died?.Invoke();
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
                spriteRend.color = BaseColor;
                yield return new WaitForSeconds(iFramesDuration / (numberOffFlashes * 2));
            }
            Physics2D.IgnoreLayerCollision(playerLayerNumber, enemyLayerNumber, false);
            isInvulnerable = false;
        }

        yield break;
    }

}
