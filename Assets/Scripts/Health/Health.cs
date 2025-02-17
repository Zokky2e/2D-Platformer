using Assets.Scripts;
using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header ("Health")]
    public float startingHealth;
    public float currentHealth { get; private set; }

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
        currentHealth = startingHealth;
        spriteRend = GetComponent<SpriteRenderer>();
        playerLayerNumber = (int)Math.Log(playerLayer.value, 2);
        enemyLayerNumber = (int)Math.Log(enemyLayer.value, 2);
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);
        if (currentHealth > 0)
        {
            entity.TakeDamage();
            StartCoroutine(Invunerability());
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
