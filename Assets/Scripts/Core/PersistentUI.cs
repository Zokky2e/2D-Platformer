using UnityEngine;

public class PersistentUI : MonoBehaviour
{
    private static PersistentUI instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep UI alive across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates in new scenes
        }
    }
}
