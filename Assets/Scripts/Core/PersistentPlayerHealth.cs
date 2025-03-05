using System.Collections;
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

    public override void TakeDamage(float _damage)
    {
        base.TakeDamage(_damage);
        if (currentHealth == 0)
        {
            StartCoroutine(DoDeathAnimation());
        }
    }

    IEnumerator DoDeathAnimation()
    {

        yield return new WaitForSeconds(2f);
        PauseMenu.Instance.CheckForPause(forcePause: true);
        yield break;
    }
}
