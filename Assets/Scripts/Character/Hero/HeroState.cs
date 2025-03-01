using System;
using System.Collections;
using Unity.Android.Gradle.Manifest;
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

    virtual public HeroState handleInput()
    {
        return this;
    }
    virtual public void Update()
    {
    }

    public HeroStates GetCurrentState()
    {
        return currentState;
    }

    public bool IsAttacking()
    {
        return Input.GetMouseButtonDown(0);
    }
}

//Default
public class IdleState : HeroState
{
    public IdleState(): base(HeroStates.Idle) { }
    override public HeroState handleInput()
    {
        if (Input.GetKeyDown("space"))
        {
            return new JumpingState();
        }

        if (Input.GetMouseButtonDown(0))
        {
            return new AttackingState();
        }

        if (Input.GetMouseButtonDown(1))
        {
            return new BlockingState();
        }

        if (Input.GetKeyDown("left shift"))
        {
            return new RollingState();
        }
        return this;
    }

    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetInteger("AnimState", 0);
    }
}

public class JumpingState : HeroState
{

    public JumpingState() : base(HeroStates.Jump) { }
    private float m_wallCooldown = 0.0f;
    override public HeroState handleInput()
    {
        if (hero.isGrounded()) return new IdleState();
        return this;
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        Jump();
    }

    public override void Update()
    {
        base.Update();
        m_wallCooldown += Time.deltaTime;
        if (m_wallCooldown > 0.5f)
        {
            if (hero.onWall() && !hero.isGrounded())
            {
                m_body2d.gravityScale = 0;
                m_body2d.linearVelocity = Vector2.zero;
            }
            else
            {
                m_body2d.gravityScale = hero.Gravity;
            }
        }
    }

    private void Jump()
    {
        m_animator.SetBool("WallSlide", false);
        m_body2d.gravityScale = hero.Gravity;
        if (hero.isGrounded())
        {
            m_animator.SetTrigger("Jump");
            m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, hero.JumpForce);
            hero.GroundSensor.Disable(0.2f);
        }
        else if (hero.onWall() && !hero.isGrounded())
        {
            if (hero.HorizontalInput == 0)
            {
                m_body2d.linearVelocity = new Vector2(-Mathf.Sign(hero.FacingDirection) * 6, 0);
            }
            else
            {
                Debug.Log("wall jump");
                m_animator.SetTrigger("Jump");
                m_body2d.gravityScale = 5f;
                m_body2d.linearVelocity = new Vector2(-Mathf.Sign(hero.FacingDirection) * hero.JumpModifierX, hero.JumpModifierY);
            }
            m_animator.SetBool("WallSlide", false);
            m_body2d.gravityScale = hero.Gravity;
            m_wallCooldown = 0;
        }
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
            m_animator.SetTrigger("Attack" + m_currentAttack);

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

        m_animator.SetTrigger("Attack" + m_currentAttack);

        yield return new WaitForSeconds(0.5f); // Adjust based on animation length

        canAttack = true;
    }
}
public class BlockingState : HeroState
{
    public BlockingState() : base(HeroStates.Block) { }
    override public HeroState handleInput()
    {
        if (Input.GetMouseButtonUp(1))
        {
            m_animator.SetBool("IdleBlock", false);
            return new IdleState();
        }
        return this;
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetTrigger("Block");
        m_animator.SetBool("IdleBlock", true);
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
            m_body2d.gravityScale = hero.Gravity;
            return new IdleState();
        }
        return this;
    }
    public override void startState(Hero hero)
    {
        base.startState(hero);
        m_animator.SetTrigger("Roll");
        Roll(hero);
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