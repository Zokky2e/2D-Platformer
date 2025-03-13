using Assets.Scripts;
using NaughtyAttributes;
using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IEntity
{
    [Header("General Settings")]
    public float damage;
    public bool isTrap = false;

    [Header("Patrol & Movement")]
    public float moveSpeed = 2f;
    public Transform[] patrolPoints;
    private int currentPointIndex = 0;
    private bool isChasing = false;

    [Header("Combat")]
    public float attackDelay = 1.5f;
    public float attackSpeedAnimation = 0.3f;
    public float attackRange = 1.5f;
    public float detectionRange = 4f;

    private Animator animator;
    private FloatingHealthBar floatingHealthBar;
    private SpriteRenderer spriteRenderer;
    private Health health;
    private bool isAttacking = false;
    private bool canAttack = true;
    private Transform player;

    public void Start()
    {
        animator = this.GetComponent<Animator>();
        floatingHealthBar = this.GetComponent<FloatingHealthBar>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = this.GetComponent<Health>();

        if (health != null)
            health.entity = this;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!isTrap) 
            StartCoroutine(Patrol());
    }
    void Update()
    {
        if (!isTrap)
        {
            if (player == null && !health || health.currentHealth <= 0) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                StopAllCoroutines();
                isChasing = true;
                ChasePlayer();
            }
            else if (isChasing)
            {
                isChasing = false;
                StartCoroutine(Patrol());
            }
        }
    }

    private IEnumerator Patrol()
    {
        while (!isChasing && patrolPoints.Length > 1)
        {
            Transform targetPoint = patrolPoints[currentPointIndex];

            while (Vector2.Distance(transform.position, targetPoint.position) > 0.1f)
            {
                animator.SetInteger("AnimState", 2);
                MoveTowards(targetPoint.position);
                yield return null;
            }

            animator.SetInteger("AnimState", 0);
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            yield return new WaitForSeconds(2f);
        }
    }

    private void MoveTowards(Vector2 target)
    {
        Vector2 direction = target - (Vector2)transform.position;
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        // Flip the sprite based on movement direction
        spriteRenderer.flipX = direction.x > 0;
    }

    private void ChasePlayer()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            // Stop moving if the enemy is within attack range
            if (distanceToPlayer > attackRange * 0.6f)
            {
                animator.SetInteger("AnimState", 2);
                MoveTowards(player.position);
            }
            else
            {
                animator.SetInteger("AnimState", 0); // Idle animation when within range
                if (canAttack && !isAttacking)
                {
                    StartCoroutine(AttackLoop(player.GetComponent<Health>()));
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && damage > 0)
        {
            if (isTrap)
            {
                collision.GetComponent<Health>().TakeDamage(damage);
            }
            else if(!isAttacking && health.currentHealth > 0)
            {
                StartCoroutine(AttackLoop(collision.GetComponent<Health>()));
            }
        }
    }

    private IEnumerator AttackLoop(Health playerHealth)
    {
        isAttacking = true;

        while (playerHealth != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerHealth.transform.position);

            if (distanceToPlayer > attackRange)
            {
                isAttacking = false; // Stop attacking when the player leaves range
                break; // Stop attacking if player moves out of range
            }
            animator.SetTrigger("Attack");
            yield return new WaitForSeconds(attackSpeedAnimation);

            if (playerHealth != null && Vector2.Distance(transform.position, playerHealth.transform.position) <= attackRange)
            {
                playerHealth.TakeDamage(damage);
            }

            yield return new WaitForSeconds(attackDelay);
        }

        isAttacking = false; // Stop attacking when the player leaves range
    }

    public void TakeDamage()
    {
        animator.SetTrigger("Hurt");
    }

    public void Die()
    {
        animator.SetTrigger("Death");
        StopAllCoroutines();
    }

    public bool IsBlocking()
    {
        return false;
    }
}
