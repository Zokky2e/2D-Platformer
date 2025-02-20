using UnityEngine;

public class PersistentPlayerHealth : Health
{
    public static PersistentPlayerHealth Instance;

    public new void Awake()
    {
        if (Instance == null)
        {
            base.Awake();
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
