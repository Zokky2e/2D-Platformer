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
    private Coroutine patrolRoutine; // The running Patrol(), so it can actually be stopped

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
            patrolRoutine = StartCoroutine(Patrol());

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
                if (!isChasing)
                {
                    isChasing = true;
                    StopPatrol();
                }
                ChasePlayer();
            }
            else if (isChasing)
            {
                isChasing = false;
                patrolRoutine = StartCoroutine(Patrol());
            }
        }
    }

    private void StopPatrol()
    {
        // StopCoroutine(Patrol()) would stop a new enumerator, not the running one
        if (patrolRoutine != null)
        {
            StopCoroutine(patrolRoutine);
            patrolRoutine = null;
        }
    }

    private IEnumerator Patrol()
    {
        while (!isChasing && patrolPoints.Length > 0)
        {
            Transform targetPoint = patrolPoints[currentPointIndex];

            while (Mathf.Abs(transform.position.x - targetPoint.position.x) > 0.1f)
            {
                animator.SetInteger(AnimatorParams.AnimState, 2);
                MoveTowards(targetPoint.position);
                yield return null;
            }

            animator.SetInteger(AnimatorParams.AnimState, 0);
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            yield return new WaitForSeconds(2f);
        }
        animator.SetInteger(AnimatorParams.AnimState, 0); // Nothing to patrol (bosses), so stand still after a chase
    }

    // Walks horizontally only: moving toward the target's height made enemies float up after a jumping player
    private void MoveTowards(Vector2 target)
    {
        target.y = transform.position.y;
        Vector2 direction = target - (Vector2)transform.position;
        transform.position = Vector2.MoveTowards(transform.position, target, stats.TotalMoveSpeed * Time.deltaTime);

        // Flip the sprite based on movement direction
        spriteRenderer.flipX = direction.x > 0;
    }

    private void ChasePlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool underPlayer = Mathf.Abs(player.position.x - transform.position.x) < 0.1f; // Player is above, out of reach

        if (distanceToPlayer > attackRange * 0.9f && !underPlayer)
        {
            animator.SetInteger(AnimatorParams.AnimState, 2);
            MoveTowards(player.position);
        }
        else
        {
            animator.SetInteger(AnimatorParams.AnimState, 0); // Idle animation

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
                if (collision.GetComponent<Health>().TakeDamage(stats.TotalDamage))
                    stats.ApplyOnHitEffects(collision.gameObject);
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
            animator.SetTrigger(AnimatorParams.Attack);
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
            if (player.GetComponent<Health>().TakeDamage(stats.TotalDamage))
                stats.ApplyOnHitEffects(player.gameObject); // Set on-hit values on the enemy's CharacterStats to use
        }
    }
    public float TakeDamage(float _damage)
    {
        animator.SetTrigger(AnimatorParams.Hurt);
        return stats.CalculateDamage(_damage);
    }

    public void Die()
    {
        animator.SetTrigger(AnimatorParams.Death);
        StopAllCoroutines();
    }

    public bool IsBlocking()
    {
        return false;
    }
}
