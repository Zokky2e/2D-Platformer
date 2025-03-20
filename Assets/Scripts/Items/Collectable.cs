using UnityEngine;

public class Collectable : MonoBehaviour
{
    public Item item;
    private SpriteRenderer spriteRenderer;
    public bool useOnPickup = false;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (item != null)
        {
            UpdateSprite();
        }
    }
    private void Start()
    {
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (item != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = item.Sprite;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Hero player = collision.GetComponent<Hero>();
            if (!useOnPickup)
            {
                InventorySystem.Instance.AddItem(item);
            }
            else if (player != null)
            {
                item.UseItem(player.stats, player.Health);
            }
            gameObject.SetActive(false);
        }
    }
}