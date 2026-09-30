using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatusEffectType
{
    Bleed,
    Poison,
    Burn
}

// Damage-over-time effects on a Health; added to the target the first time something affects it.
// Damage is per second and ticks once per second; reapplying an effect restarts it with the new values
// instead of stacking.
[RequireComponent(typeof(Health))]
public class StatusEffects : MonoBehaviour
{
    private const float TickInterval = 1f;
    private const float FlashDuration = 0.15f;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private readonly Dictionary<StatusEffectType, Coroutine> active = new();

    public bool IsAffectedBy(StatusEffectType type) => active.ContainsKey(type);

    public static void Apply(GameObject target, StatusEffectType type, float damagePerSecond, float duration)
    {
        if (damagePerSecond <= 0 || duration <= 0)
            return;
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth == null || targetHealth.CurrentHealth <= 0)
            return;
        if (!target.TryGetComponent(out StatusEffects effects))
            effects = target.AddComponent<StatusEffects>();
        effects.Begin(type, damagePerSecond, duration);
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        // Coroutines stop with the object
        active.Clear();
        if (spriteRenderer != null)
            spriteRenderer.color = health.BaseColor;
    }

    private void Begin(StatusEffectType type, float damagePerSecond, float duration)
    {
        if (active.TryGetValue(type, out Coroutine running))
            StopCoroutine(running);
        active[type] = StartCoroutine(Tick(type, damagePerSecond, duration));
    }

    private IEnumerator Tick(StatusEffectType type, float damagePerSecond, float duration)
    {
        WaitForSeconds wait = new WaitForSeconds(TickInterval);
        for (float elapsed = 0; elapsed < duration && health.CurrentHealth > 0; elapsed += TickInterval)
        {
            yield return wait;
            health.TakeStatusDamage(damagePerSecond * TickInterval);
            StartCoroutine(Flash(TintFor(type)));
        }
        active.Remove(type);
    }

    private IEnumerator Flash(Color tint)
    {
        if (spriteRenderer == null)
            yield break;
        spriteRenderer.color = tint;
        yield return new WaitForSeconds(FlashDuration);
        // Health's color, not the color when this component was added: that can be mid-flash (the hit that
        // applies the effect also starts the i-frame flash), which would leave the sprite tinted for good
        spriteRenderer.color = health.BaseColor;
    }

    private static Color TintFor(StatusEffectType type) => type switch
    {
        StatusEffectType.Bleed => new Color(0.75f, 0.1f, 0.1f),
        StatusEffectType.Poison => new Color(0.4f, 0.9f, 0.3f),
        _ => new Color(1f, 0.55f, 0.15f), // Burn
    };
}
