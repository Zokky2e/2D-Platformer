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
            // Level0's own hero when returning to it: deactivate so its Start (starting kit) never runs
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public override void TakeDamage(float _damage)
    {
        if (CurrentHealth <= 0)
            return; // Already dead, death sequence is running
        base.TakeDamage(_damage);
        if (CurrentHealth == 0)
        {
            StartCoroutine(DoDeathAnimation());
        }
    }

    public void AddMaxHealth(float _maxHealth)
    {
        bonusHealth += _maxHealth; // The HUD Healthbar picks up the new max on its own
    }

    IEnumerator DoDeathAnimation()
    {

        yield return new WaitForSeconds(2f);
        PauseMenu.Instance.CheckForPause(forcePause: true);
        yield break;
    }
}
