using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float damage;
    public float attackDelay = 1.5f;
    public float attackSpeedAnimation = 0.3f;
    private Animator animator; 
    private bool isAttacking = false;
    private Collider2D playerInRange;

    public void Start()
    {
        animator = this.GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && damage > 0 && !isAttacking)
        {
            playerInRange = collision;
            StartCoroutine(Attack());
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
            }
            yield return new WaitForSeconds(attackDelay);
        }
        isAttacking = false;
    }
}
