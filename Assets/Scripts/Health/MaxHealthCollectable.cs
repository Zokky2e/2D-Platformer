using UnityEngine;

public class MaxHealthCollectable : Item
{
    public float healthValue;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            InventorySystem.Instance.AddItem(this);
            gameObject.SetActive(false);
        }
    }

    public override void UseItem()
    {
        base.UseItem();
        PersistentPlayerHealth.Instance.AddMaxHealth(healthValue);
        PersistentPlayerHealth.Instance.AddHealth(healthValue);
    }
}
