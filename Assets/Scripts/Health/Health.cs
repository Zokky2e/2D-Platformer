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

    public void Awake()
    {
        CurrentHealth = MaxHealth;
        spriteRend = GetComponent<SpriteRenderer>();
        playerLayerNumber = (int)Math.Log(playerLayer.value, 2);
        enemyLayerNumber = (int)Math.Log(enemyLayer.value, 2);
    }

    public virtual void TakeDamage(float _damage)
    {
        if (!entity.IsBlocking())
        {
            _damage = entity.TakeDamage(_damage);
            if (_damage == 0)
            {
                return;
            }
            else if (CurrentHealth - _damage > 0)
            {
                StartCoroutine(Invunerability());
            }
            else
            {
                entity.Die();
            }
            CurrentHealth = Mathf.Clamp(CurrentHealth - _damage, 0, MaxHealth);
        }
    }

    public void AddHealth(float _healthAmount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + _healthAmount, 0, MaxHealth);
    }

    private IEnumerator Invunerability()
    {
        if (playerLayerNumber == 6)
        {
            Physics2D.IgnoreLayerCollision(playerLayerNumber, enemyLayerNumber, true);
            for (int i = 0; i < numberOffFlashes; i++) 
            {
                spriteRend.color = new Color(1, 0, 0, 0.9f);
                yield return new WaitForSeconds(iFramesDuration / (numberOffFlashes * 2));
                spriteRend.color = Color.white;
                yield return new WaitForSeconds(iFramesDuration / (numberOffFlashes * 2));
            }
            Physics2D.IgnoreLayerCollision(playerLayerNumber, enemyLayerNumber, false);
        }

        yield break;
    }

}
