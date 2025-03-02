using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : Healthbar
{

    new void Start()
    {
        //this.slider = this.GetComponent<Slider>();
    }

    new void Update()
    {
        healthValue.value = entityHealth.currentHealth / entityHealth.startingHealth;
    }

}
