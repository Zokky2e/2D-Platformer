using System;
using UnityEngine;

public class HeroState
{
    protected Animator m_animator;
    protected HeroStates currentState;
    virtual public void startState(Hero hero)
    {
        m_animator = hero.GetHeroAnimator();
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
        return this;
    }

    public override void startState(Hero hero)
    {
        currentState = HeroStates.Idle;
        base.startState(hero);
    }
}

public class JumpingState : HeroState
{
    override public HeroState handleInput()
    {
        return this;
    }
    public override void startState(Hero hero)
    {
        currentState = HeroStates.Jump;
        base.startState(hero);
    }
}

public class AttackingState : HeroState
{
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private Animator m_animator;
    override public HeroState handleInput()
    {
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
    }
}

public class RollingState : HeroState
{
    override public HeroState handleInput()
    {
        return this;
    }
    public override void startState(Hero hero)
    {
        currentState = HeroStates.Roll;
        base.startState(hero);
    }
}