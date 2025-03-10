using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Dynamic;
using System;
using Unity.VisualScripting;

public class Healthbar : MonoBehaviour
{
    public Health entityHealth;
    public UnityEngine.UI.Slider healthValue;
    public RectTransform healthBarFill;
    public GameObject breakpointPrefab;
    public int breakpointEveryX = 25;
    public TextMeshProUGUI healthText;
    public List<GameObject> markers = new List<GameObject>();

    public void Start()
    {
        setHealthbar();
        createBreakpoints();
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

    private void createBreakpoints()
    {

        if (PersistentPlayerHealth.Instance != null)
        {
            float maxHealth = PersistentPlayerHealth.Instance.startingHealth;
            float currentHealth = PersistentPlayerHealth.Instance.currentHealth;
            int currentBreakpoint = breakpointEveryX;
            while (currentBreakpoint < currentHealth)
            {
                float normalizedPos = (float)currentBreakpoint / maxHealth;
                CreateBreakpoint(normalizedPos);
                currentBreakpoint += breakpointEveryX;
            }
        }
    }

    private void CreateBreakpoint(float normalizedPos)
    {

        GameObject marker = Instantiate(breakpointPrefab, healthBarFill);
        marker.transform.localPosition = new Vector3(normalizedPos * healthBarFill.rect.width, 0, 0);
        markers.Add(marker);
    }
}
