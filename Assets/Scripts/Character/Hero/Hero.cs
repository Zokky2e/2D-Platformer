using UnityEngine;
using Assets.Scripts;
using System.Collections.Generic;

public enum HeroStates
{
    Idle = 0,
    Run,
    Jump,
    Roll,
    Attack,
    Block,
    Dead,
    LedgeGrab // New states go at the end: the values are serialized as integers
}

public class Hero : MonoBehaviour, IEntity {

    [SerializeField] float      m_jumpForce = 7.5f;
    public float JumpForce
    {
        get
        {
            return m_jumpForce;
        }
    }
    [SerializeField] float      m_rollForce = 6.0f;
    public float RollForce
    {
        get
        {
            return m_rollForce;
        }
    }
    [SerializeField] float      gravity = 1;
    public float Gravity
    {
        get
        {
            return gravity;
        }
    }

    [Header("Jump feel")]
    [Tooltip("Seconds after running off a ledge during which a jump still works")]
    [SerializeField] float      coyoteTime = 0.1f;
    [Tooltip("Seconds a jump press is remembered, so pressing just before landing still jumps")]
    [SerializeField] float      jumpBufferTime = 0.12f;
    [Tooltip("Share of the upward speed kept when Space is released during a jump; lower makes taps shorter")]
    [SerializeField] float      jumpCutMultiplier = 0.5f;
    public float JumpCutMultiplier => jumpCutMultiplier;

    [Header("Walls")]
    [Tooltip("Fastest the hero slides down a wall")]
    [SerializeField] float      wallSlideSpeed = 2.5f;
    public float WallSlideSpeed => wallSlideSpeed;
    [Tooltip("Launch velocity of a wall jump: x away from the wall, y up")]
    [SerializeField] Vector2    wallJumpVelocity = new Vector2(6f, 9f);
    public Vector2 WallJumpVelocity => wallJumpVelocity;
    [Tooltip("Seconds after a wall jump before steering takes over again, so the push away isn't cancelled")]
    [SerializeField] float      wallJumpControlLock = 0.2f;
    [Tooltip("How far beside the collider walls and ledges are detected")]
    [SerializeField] float      wallCheckDistance = 0.1f;

    [Header("Ledges")]
    [Tooltip("How far the top of the collider sits above the ledge's surface while hanging")]
    [SerializeField] float      ledgeHangOffset = 0.15f;
    [Tooltip("Seconds the pull-up onto a ledge takes")]
    [SerializeField] float      pullUpDuration = 0.2f;
    public float PullUpDuration => pullUpDuration;
    [Tooltip("Seconds after dropping or jumping off a ledge before a ledge can be grabbed again")]
    [SerializeField] float      ledgeRegrabDelay = 0.3f;

    // How far below the hands a ledge's top can be and still be grabbed (the hero is then snapped to it)
    private const float LedgeGrabRange = 0.3f;

    [SerializeField] bool       m_noBlood = false;
    public bool NoBlood
    {
        get
        {
            return m_noBlood;
        }
    }
    [SerializeField] GameObject m_slideDust;
    private Animator            m_animator;
    public Animator Animator
    {
        get
        {
            return m_animator;
        }
    }
    private Rigidbody2D         m_body2d;
    public Rigidbody2D Body2D
    {
        get
        {
            return m_body2d;
        }
    }
    private Sensor_HeroKnight   m_groundSensor;
    public Sensor_HeroKnight GroundSensor
    {
        get
        {
            return m_groundSensor;
        }
    }
    private Sensor_HeroKnight   m_wallSensorR1;
    private Sensor_HeroKnight   m_wallSensorR2;
    private Sensor_HeroKnight   m_wallSensorL1;
    private Sensor_HeroKnight   m_wallSensorL2;
    private BoxCollider2D boxCollider;
    private SpriteRenderer m_spriteRenderer;
    private Health playerHealth;
    private Mana m_mana;
    private WeaponSensor m_weaponSensor;
    public CharacterStats stats;

    public Health Health
    {
        get
        {
            return playerHealth;
        }
    }
    public float CurrentHealth
    {
        get
        {
            return playerHealth.CurrentHealth;
        }
    }
    [SerializeField] private LayerMask groundLayer;
    private int                 m_facingDirection = 1;
    public int FacingDirection
    {
        get
        {
            return m_facingDirection;
        }
    }
    private float               m_delayToIdle = 0.0f;
    private float               m_horizontalInput;
    public float HorizontalInput
    {
        get
        {
            return m_horizontalInput;
        }
    }

    // Contacts and timers for the movement states, updated once per frame in Update
    private bool                m_isGrounded;
    private int                 m_wallSide;
    private float               m_lastGroundedTime = float.NegativeInfinity;
    private float               m_lastJumpPressedTime = float.NegativeInfinity;
    private int                 m_lastWallSide;
    private float               m_lastWallTime = float.NegativeInfinity;
    private int                 m_wallJumpSide;
    private float               m_controlLockUntil;
    private float               m_ledgeGrabBlockedUntil;

    public bool IsGrounded => m_isGrounded;
    // 1 or -1 while a wall covers the whole body on that side, 0 otherwise
    public int WallSide => m_wallSide;
    public bool InCoyoteTime => Time.time - m_lastGroundedTime <= coyoteTime;
    public bool JumpBuffered => Time.time - m_lastJumpPressedTime <= jumpBufferTime;
    public void ConsumeJumpBuffer() => m_lastJumpPressedTime = float.NegativeInfinity;
    public bool HorizontalControlLocked => Time.time < m_controlLockUntil;
    public bool CanGrabLedge => Time.time >= m_ledgeGrabBlockedUntil;
    public void BlockLedgeGrab() => m_ledgeGrabBlockedUntil = Time.time + ledgeRegrabDelay;
    // True for the wall a wall jump just launched from, until steering returns
    public bool JustWallJumpedFrom(int side) => HorizontalControlLocked && side == m_wallJumpSide;
    // A wall touched within the last coyoteTime seconds that can be wall-jumped from, 0 if none
    public int RecentWallSide =>
        Time.time - m_lastWallTime <= coyoteTime && !JustWallJumpedFrom(m_lastWallSide) ? m_lastWallSide : 0;

    // A ledge the hero can hang from: where to hang, and where standing on top of it would be
    public struct Ledge
    {
        public int side;              // 1 = the ledge is to the right, -1 = to the left
        public Vector2 hangPosition;  // Transform position while hanging
        public Vector2 climbPosition; // Transform position standing on top
        public bool canClimb;         // False when something blocks the space on top
    }

    private HeroState state;
    private List<HeroStates> noMovementStates = new List<HeroStates>() { HeroStates.Roll, HeroStates.Dead, HeroStates.LedgeGrab };

    // Use this for initialization
    void Start ()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        playerHealth = GetComponent<Health>();
        stats = GetComponent<CharacterStats>();
        playerHealth.entity = this;
        m_body2d.gravityScale = gravity;
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
        state = new IdleState();
        state.startState(this);
        // Mana isn't on the prefab yet; it must exist before equipment with mana bonuses is applied
        if (!TryGetComponent(out m_mana))
            m_mana = gameObject.AddComponent<Mana>();
        m_weaponSensor = GetComponentInChildren<WeaponSensor>();
        // Continue the saved game, or start a new one with the default kit
        if (!SaveSystem.Instance.RestorePlayer(this))
        {
            ItemSystem.Instance.AddToPlayerInventory(new[] {18, 18, 19, 20 });
            ItemSystem.Instance.AddAndEquipOnPlayer(new[] {69, 420, 1337});
            playerHealth.SetHealth(playerHealth.MaxHealth); // Full health including the kit's bonuses
        }
    }
    void Update()
    {
        if (!CanMove())
        {
            m_horizontalInput = 0; // FixedUpdate would keep applying the last input during dialog
            if (state.GetCurrentState() != HeroStates.Idle)
                ChangeState(new IdleState());
            m_animator.SetInteger(AnimatorParams.AnimState, 0); // Stop the run animation
            return; // Disable movement if dialog is active
        }
        // -- Handle input and movement --
        m_horizontalInput = GameInput.Horizontal;
        if (GameInput.JumpPressed)
            m_lastJumpPressedTime = Time.time; // Buffered: the states decide when it turns into a jump
        UpdateContacts();
        handleInput();
        // Swap direction of sprite depending on walk direction, unless the state faces the hero itself
        if (!state.ControlsFacing)
        {
            if (m_horizontalInput > 0)
                SetFacing(1);
            else if (m_horizontalInput < 0)
                SetFacing(-1);
        }

        //Run
        if (Mathf.Abs(m_horizontalInput) > Mathf.Epsilon)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger(AnimatorParams.AnimState, 1);
        }
        //Idle
        else
        {
            // Prevents flickering transitions to idle
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger(AnimatorParams.AnimState, 0);
        }

        m_animator.SetBool(AnimatorParams.Grounded, m_isGrounded);
        state.Update();
    }

    void FixedUpdate()
    {
        m_animator.SetFloat(AnimatorParams.AirSpeedY, m_body2d.linearVelocity.y);
        // Steering sets the horizontal speed directly, except while rolling, dead, hanging or just after a wall jump
        if (!noMovementStates.Contains(state.GetCurrentState()) && !HorizontalControlLocked)
        {
            m_body2d.linearVelocity = new Vector2(m_horizontalInput * stats.TotalMoveSpeed, m_body2d.linearVelocity.y);
        }
    }

    void handleInput()
    {
        HeroState newState = state.handleInput();
        if (CurrentHealth <= 0 && state.GetCurrentState() != HeroStates.Dead)
            newState = new DeadState(); // From any state, for example killed in mid-air
        if (state.GetCurrentState() != newState.GetCurrentState())
            ChangeState(newState);
    }

    private void ChangeState(HeroState newState)
    {
        state.exitState();
        state = newState;
        state.startState(this);
    }

    // Ground and wall contact, checked once per frame for the states to read
    private void UpdateContacts()
    {
        m_isGrounded = isGrounded();
        if (m_isGrounded)
            m_lastGroundedTime = Time.time;
        m_wallSide = m_isGrounded ? 0 : TouchingWallSide();
        if (m_wallSide != 0)
        {
            m_lastWallSide = m_wallSide;
            m_lastWallTime = Time.time;
        }
    }

    // Box-casts a strip a little narrower than the collider (by more than the physics contact gap), so a wall
    // beside the hero doesn't count as ground but standing on the very edge of a platform still does
    public bool isGrounded()
    {
        Bounds bounds = boxCollider.bounds;
        Vector2 size = new Vector2(bounds.size.x - 0.04f, 0.05f);
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + size.y * 0.5f);
        return Physics2D.BoxCast(origin, size, 0f, Vector2.down, 0.05f, groundLayer).collider != null;
    }

    // A wall counts only if it covers both the lower and the upper body. A platform corner that reaches part
    // of the body is a ledge (FindLedge), not something to slide down or stick to
    private int TouchingWallSide()
    {
        if (WallAt(m_facingDirection))
            return m_facingDirection;
        if (WallAt(-m_facingDirection))
            return -m_facingDirection;
        return 0;
    }

    private bool WallAt(int side)
    {
        Bounds bounds = boxCollider.bounds;
        float edgeX = side > 0 ? bounds.max.x : bounds.min.x;
        Vector2 direction = new Vector2(side, 0f);
        return Physics2D.Raycast(new Vector2(edgeX, bounds.min.y + bounds.size.y * 0.25f), direction, wallCheckDistance, groundLayer)
            && Physics2D.Raycast(new Vector2(edgeX, bounds.max.y - 0.1f), direction, wallCheckDistance, groundLayer);
    }

    // Finds a ledge on the given side whose top is about level with the hero's hands (the top of the collider)
    public bool FindLedge(int side, out Ledge ledge)
    {
        ledge = default;
        Bounds bounds = boxCollider.bounds;
        Vector2 direction = new Vector2(side, 0f);
        float edgeX = side > 0 ? bounds.max.x : bounds.min.x;
        // The ledge's side, just below where the hands grab
        float lowestTopY = bounds.max.y - ledgeHangOffset - LedgeGrabRange;
        RaycastHit2D wall = Physics2D.Raycast(new Vector2(edgeX, lowestTopY), direction, wallCheckDistance, groundLayer);
        if (!wall)
            return false;
        // Its top, searched downward from just above the head. Starting inside ground (distance 0) means
        // the wall continues upward, so there is no ledge here
        float searchFromY = bounds.max.y + 0.05f;
        Vector2 searchFrom = new Vector2(wall.point.x + side * 0.05f, searchFromY);
        RaycastHit2D top = Physics2D.Raycast(searchFrom, Vector2.down, searchFromY - lowestTopY, groundLayer);
        if (!top || top.distance <= 0f || top.normal.y < 0.7f)
            return false;

        Vector3 position = transform.position;
        float surfaceY = top.point.y;
        float toTop = bounds.max.y - position.y;
        float toBottom = position.y - bounds.min.y;
        float toNearSide = side > 0 ? bounds.max.x - position.x : position.x - bounds.min.x;
        float toFarSide = side > 0 ? position.x - bounds.min.x : bounds.max.x - position.x;
        ledge.side = side;
        // Flush against the ledge's side, with the top of the collider just above its surface
        ledge.hangPosition = new Vector2(wall.point.x - side * (toNearSide + 0.02f), surfaceY + ledgeHangOffset - toTop);
        // Standing on the surface, fully past the edge
        ledge.climbPosition = new Vector2(wall.point.x + side * (toFarSide + 0.05f), surfaceY + toBottom + 0.02f);
        Vector2 standingCenter = ledge.climbPosition + (Vector2)(bounds.center - position);
        ledge.canClimb = !Physics2D.OverlapBox(standingCenter, (Vector2)bounds.size - new Vector2(0.05f, 0.05f), 0f, groundLayer);
        return true;
    }

    // Moves the hero and updates physics right away (Auto Sync Transforms is off), so the next ground and
    // wall checks see the new position
    public void Teleport(Vector2 position)
    {
        transform.position = position;
        Physics2D.SyncTransforms();
    }

    public void SetFacing(int direction)
    {
        m_facingDirection = direction;
        m_spriteRenderer.flipX = direction < 0;
    }

    // Faces away from the wall and ignores steering for wallJumpControlLock seconds
    public void StartWallJump(int wallSide)
    {
        SetFacing(-wallSide);
        m_wallJumpSide = wallSide;
        m_controlLockUntil = Time.time + wallJumpControlLock;
    }

    // How the equipped weapon attacks (no weapon fights like a sword)
    public WeaponProfile Weapon
    {
        get
        {
            Item weapon = EquipmentSystem.Instance.GetItem(EquipmentSlot.Weapon);
            return WeaponProfile.For(weapon != null ? weapon.WeaponType : WeaponType.Sword);
        }
    }

    // The moment an attack connects. Bows loose an arrow and magic weapons a bolt (paid in mana; without
    // enough, the staff or wand just hits like a melee weapon). Everything else hits what's within reach
    public void Strike(WeaponProfile weapon)
    {
        bool shoots = weapon.Projectile != ProjectileKind.None
            && (weapon.ManaCost <= 0f || (m_mana != null && m_mana.TrySpend(weapon.ManaCost)));
        if (shoots)
        {
            float damage = stats.TotalDamage;
            if (weapon.Projectile == ProjectileKind.MagicBolt)
                damage += stats.TotalMagicPower;
            Vector2 origin = (Vector2)transform.position + new Vector2(m_facingDirection * 0.6f, 0.7f);
            Projectile.Launch(weapon.Projectile, origin, m_facingDirection, damage, stats, m_spriteRenderer);
        }
        else if (m_weaponSensor != null)
        {
            m_weaponSensor.Strike(stats.TotalDamage, weapon.Reach);
        }
    }

    public bool CanMove()
    {
        return !DialogSystem.Instance.DialogActive && !DialogSystem.Instance.InputConsumedThisFrame;
    }

    public float TakeDamage(float _damage)
    {
        if (!IsBlocking())
        {
            float newDamage = stats.CalculateDamage(_damage);
            if (newDamage <= 0)
                return 0;
            m_animator.SetTrigger(AnimatorParams.Hurt);
            return newDamage;
        }
        return 0;
    }

    public void Die()
    {
    }

    public bool IsBlocking()
    {
        return GetCurrentHeroState() == HeroStates.Block || GetCurrentHeroState() == HeroStates.Roll;
    }


    public HeroStates GetCurrentHeroState()
    {
        return state.GetCurrentState();
    }

    // Animation Events
    // Called in slide animation.
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            // Set correct arrow spawn position
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Turn arrow in correct direction
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
