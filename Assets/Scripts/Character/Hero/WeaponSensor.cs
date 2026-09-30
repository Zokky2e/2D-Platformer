using System.Collections.Generic;
using UnityEngine;

// The hero's melee hitbox: the trigger collider on the AttackSensor child, turned with the hero's facing.
// A swing hits every enemy inside it at the moment the swing connects (AttackingState -> Hero.Strike), each
// once. It queries the physics world then instead of waiting for trigger callbacks, which don't arrive while
// both bodies are asleep and used to limit a whole combo to one hit.
class WeaponSensor : MonoBehaviour
{
    private Hero player;
    private BoxCollider2D hitbox;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<Health> struck = new HashSet<Health>();

    public void Awake()
    {
        // WeaponSensor sits on a child of the hero it belongs to
        player = GetComponentInParent<Hero>();
        hitbox = GetComponent<BoxCollider2D>();
    }

    // Damages each enemy in reach once. Reach stretches the hitbox forward (1 = its size on the prefab)
    public void Strike(float damage, float reach)
    {
        FaceWithHero(); // The hero may have turned toward the cursor this frame, before this Update ran
        Vector2 localCenter = hitbox.offset + new Vector2(hitbox.size.x * (reach - 1f) * 0.5f, 0f);
        Vector2 center = transform.TransformPoint(localCenter);
        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(hitbox.size.x * reach * Mathf.Abs(scale.x), hitbox.size.y * Mathf.Abs(scale.y));

        Physics2D.OverlapBox(center, size, 0f, ContactFilter2D.noFilter, overlaps);
        struck.Clear();
        foreach (Collider2D collider in overlaps)
        {
            if (!collider.CompareTag("Enemy") || !collider.TryGetComponent(out Health target) || !struck.Add(target))
                continue;
            if (target.TakeDamage(damage))
                player.stats.ApplyOnHitEffects(collider.gameObject); // Bleed/poison/burn from equipment
        }
    }

    public void Update()
    {
        FaceWithHero();
    }

    private void FaceWithHero()
    {
        float rotationAngle = player.FacingDirection == -1 ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
    }
}
