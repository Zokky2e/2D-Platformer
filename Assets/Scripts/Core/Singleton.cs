using UnityEngine;

// Marks a Singleton that has no prefab and is created on first use. Any other Singleton created that way logs
// a warning, because it means its prefab is missing from the loaded scenes or was destroyed.
[System.AttributeUsage(System.AttributeTargets.Class)]
public class AutoCreatedSingletonAttribute : System.Attribute
{
}

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting = false; // Prevents creating instances on exit

    public static T Instance
    {
        get
        {
            if (_isQuitting)
            {
                return null;
            }

            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<T>();

                if (_instance == null)
                {
                    if (!System.Attribute.IsDefined(typeof(T), typeof(AutoCreatedSingletonAttribute)))
                        Debug.LogWarning($"No {typeof(T).Name} found, so an empty one was created. Its prefab is missing from the loaded scenes or was destroyed (enter Play mode from Level0).");
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }
    // Whether an instance exists. Unlike Instance it never creates one, so it's safe in OnDestroy
    public static bool HasInstance => !_isQuitting && _instance != null;

    // True on a duplicate that is being removed; subclasses should return right after base.Awake()
    protected bool IsDuplicate { get; private set; }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // Destroy is deferred to the end of the frame; deactivate first so the duplicate
            // (e.g. from reloading Level0) gets no OnEnable/Start/Update calls in the meantime
            IsDuplicate = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
    private void OnApplicationQuit()
    {
        _isQuitting = true; // Prevents recreation during shutdown
    }
}
