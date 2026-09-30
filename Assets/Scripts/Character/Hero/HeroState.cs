using System;
using System.Collections;
using UnityEngine;

public class HeroState
{
    protected Animator m_animator;
    protected Rigidbody2D m_body2d;
    protected Hero hero;
    protected HeroStates currentState;
    protected HeroState(HeroStates state)
    {
        currentState = state;
    }
    virtual public void startState(Hero hero)
    {
        this.hero = hero;
        m_animator = hero.Animator;
        m_body2d = hero.Body2D;
    }

    // Called when the hero leaves this state for any reason (including dialog and death), to undo its setup
    virtual public void exitState()
    {
    }

    virtual public HeroState handleInput()
    {
        return this;
    }
    virtual public void Update()
    {
    }

    // True while the state sets the facing direction itself instead of following the steering
    virtual public bool ControlsFacing => false;

    public HeroStates GetCurrentState()
    {
        return currentState;
    }

    public bool IsAttacking()
    {
        return GameInput.AttackPressed;
    }
}

//Default
public class IdleState : HeroState
{
    public IdleState(): base(HeroStates.Idle) { }
    override public HeroState handleInput()
    {
        if (hero.CurrentHealth == 0)
        {
            return new DeadState();
        }
        // Also a press made just before landing, and a jump just after running off a ledge (coyote time)
        if (hero.JumpBuffered)
        {
            return new JumpingState();
        }
        if (!hero.IsGrounded)
        {
            return new JumpingState(AirStart.Fall);
        }

        if (GameInput.AttackPressed)
        {
            return new AttackingState();
        }

        if (GameInput.BlockPressed && hero.stats.canUseBlock)
        {
            return new BlockingState();
        }

        if (GameInput.RollPressed)
        {
            return new RollingState();
        }
        return this;
    }

    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetInteger(AnimatorParams.AnimState, 0);
    }
}

// How the hero got into the air
public enum AirStart
{
    Fall,     // Walked off an edge or dropped from a ledge
    Jump,     // Jumped from the ground (or within coyote time)
    WallJump  // Jumped off a wall or away from a ledge
}

// In the air: jumping, falling, wall sliding and wall jumping
public class JumpingState : HeroState
{
    private readonly AirStart start;
    private readonly int startWallSide;
    private bool usedGroundJump; // The ground (or coyote) jump is spent for this time in the air
    private bool canCutJump;     // Rising from a jump: releasing Space now cuts it short
    private bool isWallSliding;

    public JumpingState(AirStart start = AirStart.Jump, int wallSide = 0) : base(HeroStates.Jump)
    {
        this.start = start;
        startWallSide = wallSide;
    }

    public override bool ControlsFacing => isWallSliding || hero.HorizontalControlLocked;

    override public HeroState handleInput()
    {
        // Landed. Only when not rising: right after takeoff the ground is still within reach of the check
        if (hero.IsGrounded && m_body2d.linearVelocity.y <= 0.01f)
            return new IdleState();
        // Catch a ledge on the way down, unless steering away from it
        if (m_body2d.linearVelocity.y <= 0f && hero.CanGrabLedge && !hero.HorizontalControlLocked
            && GameInput.HorizontalRaw != -hero.FacingDirection
            && hero.FindLedge(hero.FacingDirection, out Hero.Ledge ledge))
            return new LedgeGrabState(ledge);
        return this;
    }

    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_body2d.gravityScale = hero.Gravity;
        if (start == AirStart.Jump)
            TryGroundJump();
        else if (start == AirStart.WallJump)
            WallJump(startWallSide);
    }

    public override void exitState()
    {
        m_animator.SetBool(AnimatorParams.WallSlide, false);
    }

    public override void Update()
    {
        base.Update();
        if (hero.JumpBuffered)
        {
            if (!usedGroundJump && hero.InCoyoteTime)
                TryGroundJump();
            else if (hero.RecentWallSide != 0)
                WallJump(hero.RecentWallSide);
            // Otherwise the press stays buffered for a moment, so it fires if the hero lands in time
        }
        UpdateWallSlide();
        if (canCutJump)
        {
            Vector2 velocity = m_body2d.linearVelocity;
            if (velocity.y <= 0f)
                canCutJump = false;
            else if (!GameInput.JumpHeld)
            {
                m_body2d.linearVelocity = new Vector2(velocity.x, velocity.y * hero.JumpCutMultiplier);
                canCutJump = false;
            }
        }
    }

    private void TryGroundJump()
    {
        if (usedGroundJump || !(hero.IsGrounded || hero.InCoyoteTime))
            return;
        hero.ConsumeJumpBuffer();
        usedGroundJump = true;
        canCutJump = true;
        m_animator.SetTrigger(AnimatorParams.Jump);
        m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, hero.JumpForce);
    }

    // Up and away from the wall on the given side. Steering is locked for a moment (Hero.FixedUpdate), so
    // holding toward the wall doesn't cancel the push; after that the hero can drift back to climb it
    private void WallJump(int wallSide)
    {
        hero.ConsumeJumpBuffer();
        hero.StartWallJump(wallSide);
        usedGroundJump = true;
        canCutJump = true;
        isWallSliding = false;
        m_animator.SetBool(AnimatorParams.WallSlide, false);
        m_animator.SetTrigger(AnimatorParams.Jump);
        m_body2d.linearVelocity = new Vector2(-wallSide * hero.WallJumpVelocity.x, hero.WallJumpVelocity.y);
    }

    // Sliding means touching a wall while falling without steering away from it; the fall speed is capped
    private void UpdateWallSlide()
    {
        int wall = hero.WallSide;
        isWallSliding = wall != 0 && m_body2d.linearVelocity.y <= 0f
            && GameInput.HorizontalRaw != -wall && !hero.JustWallJumpedFrom(wall);
        if (isWallSliding)
        {
            hero.SetFacing(wall);
            Vector2 velocity = m_body2d.linearVelocity;
            if (velocity.y < -hero.WallSlideSpeed)
                m_body2d.linearVelocity = new Vector2(velocity.x, -hero.WallSlideSpeed);
        }
        m_animator.SetBool(AnimatorParams.WallSlide, isWallSliding);
    }
}

// Hanging from a ledge. Jump, or holding toward the ledge, pulls up onto it; down drops; jump while holding
// away from the ledge jumps off it
public class LedgeGrabState : HeroState
{
    private const float MinHangBeforeClimb = 0.2f; // So a direction held while falling doesn't skip the hang
    private const float ClimbUpShare = 0.6f;       // Part of the pull-up spent rising beside the wall

    private readonly Hero.Ledge ledge;
    private float hangTime;
    private float climbTime = -1f; // Below 0 until the pull-up starts

    public LedgeGrabState(Hero.Ledge ledge) : base(HeroStates.LedgeGrab)
    {
        this.ledge = ledge;
    }

    public override bool ControlsFacing => true;

    public override void startState(Hero hero)
    {
        base.startState(hero);
        hero.SetFacing(ledge.side);
        m_body2d.gravityScale = 0f;
        m_body2d.linearVelocity = Vector2.zero;
        hero.Teleport(ledge.hangPosition);
        m_animator.SetBool(AnimatorParams.WallSlide, false);
        m_animator.SetBool(AnimatorParams.LedgeGrab, true);
    }

    public override void exitState()
    {
        m_body2d.gravityScale = hero.Gravity;
        m_animator.SetBool(AnimatorParams.LedgeGrab, false);
    }

    override public HeroState handleInput()
    {
        if (climbTime >= 0f)
            return climbTime >= hero.PullUpDuration ? new IdleState() : this;

        float steering = GameInput.HorizontalRaw;
        if (GameInput.DownHeld)
        {
            hero.BlockLedgeGrab();
            return new JumpingState(AirStart.Fall);
        }
        if (hero.JumpBuffered && steering == -ledge.side)
        {
            hero.BlockLedgeGrab();
            return new JumpingState(AirStart.WallJump, ledge.side);
        }
        if (ledge.canClimb && (hero.JumpBuffered || (steering == ledge.side && hangTime >= MinHangBeforeClimb)))
        {
            hero.ConsumeJumpBuffer();
            climbTime = 0f;
        }
        return this;
    }

    public override void Update()
    {
        base.Update();
        hangTime += Time.deltaTime;
        m_body2d.linearVelocity = Vector2.zero;
        if (climbTime < 0f)
            return;

        // Up beside the wall first, then over onto the ledge, so the collider never cuts through the corner
        climbTime += Time.deltaTime;
        float t = Mathf.Clamp01(climbTime / hero.PullUpDuration);
        Vector2 besideTop = new Vector2(ledge.hangPosition.x, ledge.climbPosition.y);
        Vector2 position = t < ClimbUpShare
            ? Vector2.Lerp(ledge.hangPosition, besideTop, t / ClimbUpShare)
            : Vector2.Lerp(besideTop, ledge.climbPosition, (t - ClimbUpShare) / (1f - ClimbUpShare));
        if (t >= 1f)
            hero.Teleport(position); // Physics must see the final spot before the next ground check
        else
            hero.transform.position = position;
    }
}

public class AttackingState : HeroState
{
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private bool canAttack;
    public AttackingState() : base(HeroStates.Attack) { }
    override public HeroState handleInput()
    {
        // If attack animation is done, go back to idle
        if (IsAttacking() && canAttack)
        {
            return new AttackingState(); // Restart attack if valid
        }
        if (m_timeSinceAttack > 0.5f) return new IdleState();
        return this;
    }

    override public void Update()
    {
        base.Update();
        m_timeSinceAttack += Time.deltaTime;

        if (IsAttacking() && !PauseMenu.GameIsPaused)
        {
            m_currentAttack++;

            // Loop back to one after third attack
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Reset Attack combo if time since last attack is too large
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger(AnimatorParams.HeroAttack(m_currentAttack));

            // Reset timer
            m_timeSinceAttack = 0.0f;
        }
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        if (!PauseMenu.GameIsPaused)
        {
            hero.StartCoroutine(AttackRoutine());
        }
    }
    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        m_currentAttack++;
        if (m_currentAttack > 3) m_currentAttack = 1;

        m_animator.SetTrigger(AnimatorParams.HeroAttack(m_currentAttack));

        yield return new WaitForSeconds(0.5f); // Adjust based on animation length

        canAttack = true;
    }
}
public class BlockingState : HeroState
{
    public BlockingState() : base(HeroStates.Block) { }
    override public HeroState handleInput()
    {
        if (!GameInput.BlockHeld) // Not "released this frame", which a skipped frame could miss
        {
            return new IdleState();
        }
        return this;
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetTrigger(AnimatorParams.Block);
        m_animator.SetBool(AnimatorParams.IdleBlock, true);
    }

    public override void exitState()
    {
        m_animator.SetBool(AnimatorParams.IdleBlock, false); // Also when a dialog or death interrupts the block
    }
}

public class RollingState : HeroState
{
    private float m_rollDuration = 8.0f / 14.0f;
    private float m_rollCurrentTime = 0f;
    public RollingState() : base(HeroStates.Roll) { }
    override public HeroState handleInput()
    {
        // Disable rolling if timer extends duration
        if (m_rollCurrentTime > m_rollDuration)
        {
            return new IdleState();
        }
        return this;
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetTrigger(AnimatorParams.Roll);
        Roll(hero);
    }

    public override void exitState()
    {
        m_body2d.gravityScale = hero.Gravity; // Also when a dialog interrupts the roll
    }

    public override void Update()
    {
        base.Update();
        m_rollCurrentTime += Time.deltaTime;
    }

    private void Roll(Hero hero)
    {
        m_body2d.gravityScale = 0.1f;
        m_body2d.linearVelocity = new Vector2(hero.FacingDirection * hero.RollForce, m_body2d.linearVelocity.y);
    }
}

public class DeadState : HeroState
{
    public DeadState() : base (HeroStates.Dead) { }

    override public HeroState handleInput()
    {
        if (hero.CurrentHealth != 0)
        {
            m_animator.SetTrigger(AnimatorParams.Revive);
            return new IdleState();
        }
        return this;
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetBool(AnimatorParams.NoBlood, hero.NoBlood);
        m_animator.SetTrigger(AnimatorParams.Death);
        // Steering stops while dead, and the hero's collider is frictionless, so stop here instead of sliding
        m_body2d.linearVelocity = new Vector2(0f, m_body2d.linearVelocity.y);
    }
}
