using UnityEngine;

public class Collectable : MonoBehaviour
{
    public Item item;
    private SpriteRenderer spriteRenderer;
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
            InventorySystem.Instance.AddItem(item);
            gameObject.SetActive(false);
        }
    }
}