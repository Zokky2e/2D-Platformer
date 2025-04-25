using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : Healthbar
{
    new void Update()
    {
        healthValue.value = entityHealth.CurrentHealth / entityHealth.MaxHealth;
    }

}
