using UnityEngine;
using TMPro;

public class Healthbar : MonoBehaviour
{
    public Health entityHealth;
    public UnityEngine.UI.Slider healthValue;
    public TextMeshProUGUI healthText;

    public void Start()
    {
        setHealthbar();
    }

    public void Update()
    {
        setHealthbar();
    }

    private void setHealthbar()
    {

        if (PersistentPlayerHealth.Instance != null)
        {
            float healthPercent = PersistentPlayerHealth.Instance.currentHealth / PersistentPlayerHealth.Instance.startingHealth;
            healthText.text = PersistentPlayerHealth.Instance.currentHealth.ToString();
            healthValue.value = healthPercent;
        }
    }
}
