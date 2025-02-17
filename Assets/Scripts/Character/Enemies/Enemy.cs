using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("hit");
        Debug.Log(collision.tag);
        if (collision.tag == "Player" && damage > 0)
        {
            collision.GetComponent<Health>().TakeDamage(damage);
        }
    }
}
