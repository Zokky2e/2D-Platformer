using System.Collections.Generic;
using UnityEngine;

// An arrow or magic bolt fired by the hero. It flies in a straight line, in any direction (arrows are aimed at
// the mouse cursor), and hits the first living enemy or wall in its path, applying the shooter's on-hit
// effects (poison, bleed, burn). It casts a ray each frame instead of using a collider, so it can't tunnel
// through thin walls at speed. The sprites are drawn in code until there is projectile art.
public class Projectile : MonoBehaviour
{
    private const float PixelsPerUnit = 32f; // Same as the hero art

    private static readonly Dictionary<ProjectileKind, Sprite> sprites = new Dictionary<ProjectileKind, Sprite>();
    private static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private float damage;
    private float speed;
    private float range;
    private Vector2 direction; // Unit length
    private float travelled;
    private CharacterStats shooter;
    private ContactFilter2D filter;
    private int groundLayer;

    public static void Launch(ProjectileKind kind, Vector2 origin, Vector2 direction, float damage, CharacterStats shooter,
        SpriteRenderer drawAbove)
    {
        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        GameObject projectileObject = new GameObject(kind.ToString());
        projectileObject.transform.position = origin;
        // The sprites point right; turn them to the flight direction
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteFor(kind);
        renderer.sortingLayerID = drawAbove.sortingLayerID;
        renderer.sortingOrder = drawAbove.sortingOrder + 1;

        Projectile projectile = projectileObject.AddComponent<Projectile>();
        projectile.damage = damage;
        projectile.direction = direction;
        projectile.shooter = shooter;
        projectile.speed = kind == ProjectileKind.Arrow ? 14f : 10f;
        projectile.range = kind == ProjectileKind.Arrow ? 12f : 9f;
    }

    private void Awake()
    {
        groundLayer = LayerMask.NameToLayer("Ground");
        filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = LayerMask.GetMask("Ground", "Enemy") };
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector2 position = transform.position;

        Physics2D.Raycast(position, direction, filter, hits, step);
        hits.Sort((a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit2D hit in hits)
        {
            GameObject target = hit.collider.gameObject;
            if (target.layer == groundLayer)
            {
                Destroy(gameObject);
                return;
            }
            // Dead enemies stay in the world; the projectile flies past them
            if (target.CompareTag("Enemy") && target.TryGetComponent(out Health health) && health.CurrentHealth > 0)
            {
                if (health.TakeDamage(damage))
                    shooter.ApplyOnHitEffects(target);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = position + direction * step;
        travelled += step;
        if (travelled >= range)
            Destroy(gameObject);
    }

    // Tiny pixel-art sprites, pointing right: f fletching, s shaft, h head, c glow, w core
    private static readonly string[] ArrowPixels =
    {
        "f.......h..",
        "ffsssssshhh",
        "f.......h..",
    };

    private static readonly string[] BoltPixels =
    {
        "...cc..",
        ".ccwwc.",
        "cccwwwc",
        ".ccwwc.",
        "...cc..",
    };

    private static Sprite SpriteFor(ProjectileKind kind)
    {
        if (sprites.TryGetValue(kind, out Sprite sprite))
            return sprite;
        string[] rows = kind == ProjectileKind.Arrow ? ArrowPixels : BoltPixels;
        int width = rows[0].Length, height = rows.Length;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < height; y++)
        {
            string row = rows[height - 1 - y]; // Texture rows go bottom-up
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, ColorFor(row[x]));
        }
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        sprites[kind] = sprite;
        return sprite;
    }

    private static Color ColorFor(char pixel) => pixel switch
    {
        'f' => new Color(0.9f, 0.9f, 0.9f),
        's' => new Color(0.55f, 0.35f, 0.2f),
        'h' => new Color(0.75f, 0.75f, 0.8f),
        'c' => new Color(0.4f, 0.8f, 1f),
        'w' => new Color(0.9f, 1f, 1f),
        _ => Color.clear,
    };
}
