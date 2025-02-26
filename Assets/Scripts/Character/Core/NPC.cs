using Unity.VisualScripting;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public bool facingToRight = true;
    private SpriteRenderer spriteRenderer;
    private Health health;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // Get the SpriteRenderer component
        health = GetComponent<Health>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Transform player = collision.transform;

            if (player.position.x < transform.position.x && facingToRight)
            {
                Flip();
            }
            else if (player.position.x > transform.position.x && !facingToRight)
            {
                Flip();
            }
        }
    }
    private void Flip()
    {
        if (health && health.currentHealth <= 0)
        {
            //TO DO: dont flip
        }
        facingToRight = !facingToRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}
