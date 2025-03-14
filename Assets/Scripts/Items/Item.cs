using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private string _name;

    [SerializeField] private string _description;

    public string Name { get { return _name; } }
    public string Description { get { return _description; } }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
