using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    public Health entityHealth;
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

    public void Update()
    {
        if (PersistentPlayerHealth.Instance != null)
        {
            float healthPercent = PersistentPlayerHealth.Instance.currentHealth / 10;
            currentHealthbar.fillAmount = healthPercent;
        }
    }
}
