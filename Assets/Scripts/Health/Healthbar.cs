using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum BarResource
{
    Health,
    Mana
}

// Health bar for the player HUD and enemies. The player's HUD bar also spawns a copy of itself below as the mana bar.
public class Healthbar : MonoBehaviour
{
    public BarResource resource = BarResource.Health;
    public Health entityHealth; // Leave empty on the HUD to follow the persistent player
    public Slider healthValue;
    public RectTransform healthBarFill;
    public GameObject breakpointPrefab;
    public int breakpointEveryX = 25;
    public TextMeshProUGUI healthText; // Optional, enemy bars have none
    public GameObject healthIcon; // Optional (the HUD's heart); hidden on the mana bar copy
    public List<GameObject> markers = new List<GameObject>();

    [Header("HUD mana bar")]
    public bool spawnManaBar = true;
    public Sprite manaFillSprite; // Falls back to a plain blue fill
    public float manaBarSpacing = 6f;

    private float shownValue = -1f;
    private float shownMax = -1f;
    private float shownShield = -1f;
    private Mana playerMana;

    private bool IsPlayerHud => entityHealth == null;

    public void Start()
    {
        if (IsPlayerHud && resource == BarResource.Health && spawnManaBar)
            CreateManaBar();
        setHealthbar();
    }

    public void Update()
    {
        setHealthbar();
    }

    private bool TryReadValues(out float current, out float max, out float shield)
    {
        current = max = shield = 0;
        if (resource == BarResource.Mana)
        {
            // The hero adds its Mana component in Start, possibly after this bar's first frame
            if (playerMana == null && PersistentPlayerHealth.Instance != null)
                PersistentPlayerHealth.Instance.TryGetComponent(out playerMana);
            if (playerMana == null)
                return false;
            current = playerMana.CurrentMana;
            max = playerMana.MaxMana;
            return true;
        }

        Health source = entityHealth != null ? entityHealth : PersistentPlayerHealth.Instance;
        if (source == null)
            return false;
        current = source.CurrentHealth;
        max = source.MaxHealth;
        shield = Mathf.Ceil(source.TotalShield);
        return true;
    }

    private void setHealthbar()
    {
        if (!TryReadValues(out float current, out float max, out float shield))
            return;

        // Equipment can change the max at any time, so rebuild the breakpoints when it does
        bool maxChanged = max != shownMax;
        if (maxChanged)
        {
            shownMax = max;
            createBreakpoints();
        }

        // Only touch the UI (and allocate the text) when something changed
        if (maxChanged || current != shownValue || shield != shownShield)
        {
            shownValue = current;
            shownShield = shield;
            healthValue.value = max > 0 ? current / max : 0;
            if (healthText != null)
                healthText.text = shield > 0 ? $"{current} (+{shield})" : current.ToString();
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

        if (breakpointPrefab == null || breakpointEveryX <= 0 || !TryReadValues(out _, out float maxValue, out _))
            return;
        int currentBreakpoint = breakpointEveryX;
        while (currentBreakpoint < maxValue)
        {
            float normalizedPos = (float)currentBreakpoint / maxValue;
            CreateBreakpoint(normalizedPos);
            currentBreakpoint += breakpointEveryX;
        }
    }

    // Anchored at a fraction of the fill's width and stretched to its height, so the marker lands in the
    // right place whatever the fill's pivot and size (the enemy bars' fill is centered, the HUD's isn't)
    private void CreateBreakpoint(float normalizedPos)
    {
        RectTransform marker = (RectTransform)Instantiate(breakpointPrefab, healthBarFill).transform;
        marker.anchorMin = new Vector2(normalizedPos, 0f);
        marker.anchorMax = new Vector2(normalizedPos, 1f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.anchoredPosition = Vector2.zero;
        marker.sizeDelta = new Vector2(marker.sizeDelta.x, 0f);
        markers.Add(marker.gameObject);
    }

    // Duplicates this bar (same frame and style) below itself and points the copy at the player's mana
    private void CreateManaBar()
    {
        Healthbar manaBar = Instantiate(this, transform.parent);
        manaBar.name = "Manabar";
        manaBar.resource = BarResource.Mana; // Set before its Start, so it doesn't spawn another bar
        // The heart and HP number belong to the health bar. The copy's references point at its own copies
        if (manaBar.healthText != null)
            manaBar.healthText.gameObject.SetActive(false);
        if (manaBar.healthIcon != null)
            manaBar.healthIcon.SetActive(false);
        manaBar.healthText = null;
        manaBar.healthIcon = null;
        manaBar.breakpointEveryX = 0;
        manaBar.markers = new List<GameObject>();

        RectTransform rect = (RectTransform)manaBar.transform;
        rect.anchoredPosition -= new Vector2(0, rect.rect.height + manaBarSpacing);

        if (manaBar.healthValue.fillRect != null && manaBar.healthValue.fillRect.TryGetComponent(out Image fill))
        {
            if (manaFillSprite != null)
                fill.sprite = manaFillSprite;
            else
                fill.color = new Color(0.35f, 0.55f, 1f);
        }
    }
}
