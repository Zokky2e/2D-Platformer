using UnityEngine;

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
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }
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
