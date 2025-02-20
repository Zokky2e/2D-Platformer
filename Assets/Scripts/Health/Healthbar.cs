using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    private Health playerHealth;
    public Image totalHealthbar;
    public Image currentHealthbar;

    public void Start()
    {
        if (PersistentPlayerHealth.Instance != null)
        {
            float healthPercent = PersistentPlayerHealth.Instance.currentHealth / 10;
            totalHealthbar.fillAmount = healthPercent;
        }
    }

    void Update()
    {
        if (PersistentPlayerHealth.Instance != null)
        {
            float healthPercent = PersistentPlayerHealth.Instance.currentHealth / 10;
            currentHealthbar.fillAmount = healthPercent;
        }
    }
}
