using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Healthbar : MonoBehaviour
{
    public Health entityHealth; // Leave empty on the HUD to follow the persistent player
    public UnityEngine.UI.Slider healthValue;
    public RectTransform healthBarFill;
    public GameObject breakpointPrefab;
    public int breakpointEveryX = 25;
    public TextMeshProUGUI healthText; // Optional, enemy bars have none
    public List<GameObject> markers = new List<GameObject>();

    private float shownHealth = -1f;
    private float shownMaxHealth = -1f;

    private Health Source => entityHealth != null ? entityHealth : PersistentPlayerHealth.Instance;

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
        Health source = Source;
        if (source == null)
            return;

        // Equipment can change max health at any time, so rebuild the breakpoints when it does
        bool maxHealthChanged = source.MaxHealth != shownMaxHealth;
        if (maxHealthChanged)
        {
            shownMaxHealth = source.MaxHealth;
            createBreakpoints();
        }

        // Only touch the UI (and allocate the text) when something changed
        if (maxHealthChanged || source.CurrentHealth != shownHealth)
        {
            shownHealth = source.CurrentHealth;
            healthValue.value = shownHealth / shownMaxHealth;
            if (healthText != null)
                healthText.text = shownHealth.ToString();
        }
    }

    public void createBreakpoints()
    {
        // Clear existing breakpoints first to avoid duplicating them
        foreach (var marker in markers)
        {
            Destroy(marker);
        }
        markers.Clear();

        Health source = Source;
        if (source != null && breakpointPrefab != null && breakpointEveryX > 0)
        {
            float maxHealth = source.MaxHealth;
            int currentBreakpoint = breakpointEveryX;
            while (currentBreakpoint < maxHealth)
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
