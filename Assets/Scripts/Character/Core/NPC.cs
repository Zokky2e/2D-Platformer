using UnityEngine;

public class NPC : MonoBehaviour
{
    public bool facingToRight = true;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // Get the SpriteRenderer component
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
        facingToRight = !facingToRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}
