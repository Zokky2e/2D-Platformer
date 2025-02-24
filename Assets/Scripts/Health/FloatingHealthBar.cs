using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : Healthbar
{
    public Slider slider;

    new void Start()
    {
        //this.slider = this.GetComponent<Slider>();
    }

    new void Update()
    {
        slider.value = entityHealth.currentHealth / entityHealth.startingHealth;
    }

}
