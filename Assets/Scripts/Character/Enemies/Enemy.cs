using Assets.Scripts;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IEntity
{
    [Header("General Settings")]
    public bool isTrap = false;

    [Header("Patrol & Movement")]
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
    public CharacterStats stats;

    private bool isAttacking = false;
    private Transform player;

    public void Start()
    {
        animator = this.GetComponent<Animator>();
        floatingHealthBar = this.GetComponent<FloatingHealthBar>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = this.GetComponent<Health>();
        stats = this.GetComponent<CharacterStats>();
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
            if (player == null) return;
            if (!health || health.CurrentHealth <= 0) return;
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                isChasing = true;
                StopCoroutine(Patrol());
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
        while (!isChasing && patrolPoints.Length > 0)
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
        transform.position = Vector2.MoveTowards(transform.position, target, stats.TotalMoveSpeed * Time.deltaTime);

        // Flip the sprite based on movement direction
        spriteRenderer.flipX = direction.x > 0;
    }

    private void ChasePlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange * 0.9f)
        {
            animator.SetInteger("AnimState", 2);
            MoveTowards(player.position);
        }
        else
        {
            animator.SetInteger("AnimState", 0); // Idle animation

            if (!isAttacking)
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && stats.TotalDamage > 0)
        {
            if (isTrap)
            {
                collision.GetComponent<Health>().TakeDamage(stats.TotalDamage);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isAttacking = false;
        }
    }
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        while (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            animator.SetTrigger("Attack");
            yield return new WaitForSeconds(attackSpeedAnimation);
            yield return new WaitForSeconds(attackDelay);
        }

        isAttacking = false;
    }


    // Animation Event: This function should be called in the animation itself
    public void DealDamage()
    {
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            player.GetComponent<Health>().TakeDamage(stats.TotalDamage);
        }
    }
    public float TakeDamage(float _damage)
    {
        animator.SetTrigger("Hurt");
        return stats.CalculateDamage(_damage);
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
