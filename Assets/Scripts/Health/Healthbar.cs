using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    public Health playerHealth;
    public Image totalHealthbar;
    public Image currentHealthbar;

    public void Start()
    {
        totalHealthbar.fillAmount = playerHealth.currentHealth / 10;
    }

    public void Update()
    {
        currentHealthbar.fillAmount = playerHealth.currentHealth / 10;
        
    }
}
