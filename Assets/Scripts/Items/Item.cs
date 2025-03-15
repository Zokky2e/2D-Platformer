using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private string _name;

    [SerializeField] private string _description;

    public Sprite Sprite { get; private set; }
    public string Name => _name;
    public string Description => _description;
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
}
