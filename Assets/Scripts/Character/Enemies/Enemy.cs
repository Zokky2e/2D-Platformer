using Assets.Scripts;
using NaughtyAttributes;
using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IEntity
{
    public float damage;
    public bool isTrap = false;

    [Header("Non Trap")] //TODO show only if isTrap is false

    public float attackDelay = 1.5f;
    public float attackSpeedAnimation = 0.3f;
    private Animator animator;
    private FloatingHealthBar floatingHealthBar;
    private Health enemyHealth;
    private bool isAttacking = false;
    private Collider2D playerInRange;

    public void Start()
    {
        animator = this.GetComponent<Animator>();
        floatingHealthBar = this.GetComponent<FloatingHealthBar>();
        enemyHealth = this.GetComponent<Health>();
        if (enemyHealth != null)
            enemyHealth.entity = this;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && damage > 0 && !isAttacking)
        {
            playerInRange = collision;
            if (isTrap)
            {
                playerInRange = collision;
                playerInRange.GetComponent<Health>().TakeDamage(damage);
                Debug.Log("Player hit!");
            }
            else
            {
                StartCoroutine(Attack());
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = null;  // Remove player reference when they leave
        }
    }

    public IEnumerator Attack()
    {
        isAttacking = true; // Prevent multiple attacks

        while (playerInRange != null) // Keep attacking while the player is inside
        {
            animator.SetTrigger("Attack");  // Start attack animation
            yield return new WaitForSeconds(attackSpeedAnimation); // Wait until attack lands
            if (playerInRange != null) // Check again to avoid null errors
            {
                playerInRange.GetComponent<Health>().TakeDamage(damage);
                Debug.Log("Player hit!");
            } else
            {
                enemyHealth.TakeDamage(1);
            }
            yield return new WaitForSeconds(attackDelay);
        }
        isAttacking = false;
    }

    public void TakeDamage()
    {
        animator.SetTrigger("Hurt");
    }

    public void Die()
    {
        animator.SetTrigger("Death");
    }

    public bool IsBlocking()
    {
        return false;
    }
}
