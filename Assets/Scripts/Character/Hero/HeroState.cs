using System;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class HeroState
{
    protected Animator m_animator;
    protected Rigidbody2D m_body2d;
    protected Hero hero;
    protected HeroStates currentState;
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
        currentState = HeroStates.Idle;
        base.startState(hero);
        m_animator.SetInteger("AnimState", 0);
    }
}

public class JumpingState : HeroState
{
    private float m_wallCooldown = 0.0f;
    override public HeroState handleInput()
    {
        if (hero.isGrounded()) return new IdleState();
        return this;
    }
    public override void startState(Hero hero)
    {
        currentState = HeroStates.Jump;
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
    override public HeroState handleInput()
    {
        // If attack animation is done, go back to idle
        if (m_timeSinceAttack > 0.5f) return new IdleState();
        return this;
    }

    override public void Update()
    {
        base.Update();
        m_timeSinceAttack += Time.deltaTime;

        if (IsAttacking())
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
        currentState = HeroStates.Attack;
        base.startState(hero);
        m_animator.SetTrigger("Attack1"); // Start first attack
    }
}
public class BlockingState : HeroState
{
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
        currentState = HeroStates.Block;
        base.startState(hero);
        m_animator.SetTrigger("Block");
        m_animator.SetBool("IdleBlock", true);
    }
}

public class RollingState : HeroState
{
    private float m_rollDuration = 8.0f / 14.0f;
    private float m_rollCurrentTime;
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
        currentState = HeroStates.Roll;
        m_rollCurrentTime = 0f;
        base.startState(hero);
        Roll(hero);
    }

    public override void Update()
    {
        base.Update();
        m_rollCurrentTime += Time.deltaTime;
    }

    private void Roll(Hero hero)
    {
        m_animator.SetTrigger("Roll");
        m_body2d.gravityScale = 0.1f;
        m_body2d.linearVelocity = new Vector2(hero.FacingDirection * hero.RollForce, m_body2d.linearVelocity.y + 2);
        m_body2d.gravityScale = hero.Gravity;
    }
}