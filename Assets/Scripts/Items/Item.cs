using UnityEngine;

public enum ItemType
{
    Consumable,
    Weapon,
    Armor,
    Accessory
}

public class Item : MonoBehaviour
{
    [SerializeField] protected string _name;

    [SerializeField] protected string _description;

    [SerializeField] protected ItemType _type;

    public Sprite Sprite { get; private set; }
    public string Name => _name;
    public string Description => _description;
    public ItemType Type => _type;


    void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Sprite = spriteRenderer.sprite;
            Debug.Log($"Item '{_name}' sprite loaded: {Sprite.name}");
        }
        else 
        {
            Debug.LogWarning($"Item '{_name}' is missing a SpriteRenderer!");
        }

    }
    void Update()
    {
        
    }

    public virtual void UseItem() { }
}
