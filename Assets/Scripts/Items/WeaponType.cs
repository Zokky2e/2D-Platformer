// Weapon kinds, set per weapon in items.json ("weaponType"). A weapon without one is a Sword, and so is
// fighting with no weapon equipped. Add new types at the end: the values are serialized as integers.
public enum WeaponType
{
    Sword,
    Dagger,
    Greatweapon,
    Bow,
    Staff,
    Wand
}

public enum ProjectileKind
{
    None,
    Arrow,
    MagicBolt
}

// How a weapon type attacks. All types still play the hero's sword swing, sped up or slowed down, toward
// the mouse cursor (see AttackingState and Hero.Strike).
public readonly struct WeaponProfile
{
    private const float BaseStrikeTime = 0.18f; // When the sword swing connects, at normal animation speed

    public readonly float SwingTime;      // Seconds before the next swing can start
    public readonly float AnimationSpeed; // Attack animation speed multiplier
    public readonly float Reach;          // Melee hitbox width, relative to the AttackSensor's collider
    public readonly bool TwoHanded;       // Can't be used with a shield
    public readonly ProjectileKind Projectile;
    public readonly float ManaCost;       // Per shot, for magic weapons
    public readonly string Summary;       // Shown at the end of the weapon's tooltip

    public float StrikeTime => BaseStrikeTime / AnimationSpeed;

    private WeaponProfile(float swingTime, float animationSpeed, float reach, bool twoHanded,
        ProjectileKind projectile, float manaCost, string summary)
    {
        SwingTime = swingTime;
        AnimationSpeed = animationSpeed;
        Reach = reach;
        TwoHanded = twoHanded;
        Projectile = projectile;
        ManaCost = manaCost;
        Summary = summary;
    }

    public static WeaponProfile For(WeaponType type) => type switch
    {
        WeaponType.Dagger => new WeaponProfile(0.3f, 1.6f, 0.7f, false, ProjectileKind.None, 0f,
            "Dagger: fast, short reach"),
        WeaponType.Greatweapon => new WeaponProfile(0.85f, 0.65f, 1.4f, true, ProjectileKind.None, 0f,
            "Greatweapon: two-handed, slow, long reach"),
        WeaponType.Bow => new WeaponProfile(0.7f, 1f, 1f, true, ProjectileKind.Arrow, 0f,
            "Bow: two-handed, shoots arrows"),
        WeaponType.Staff => new WeaponProfile(0.8f, 0.9f, 1f, true, ProjectileKind.MagicBolt, 8f,
            "Staff: two-handed, casts magic bolts (8 mana), stronger with magic power"),
        WeaponType.Wand => new WeaponProfile(0.45f, 1.3f, 1f, false, ProjectileKind.MagicBolt, 4f,
            "Wand: casts magic bolts (4 mana), stronger with magic power"),
        _ => new WeaponProfile(0.5f, 1f, 1f, false, ProjectileKind.None, 0f,
            "Sword: balanced speed and reach"),
    };
}
