using UnityEngine;
using System.Collections;
using Assets.Scripts;
using Unity.VisualScripting.FullSerializer;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.InputSystem.LowLevel;

public enum HeroStates
{
    Idle = 0,
    Run,
    Jump,
    Roll,
    Attack,
    Block,
    Dead
}

public class Hero : MonoBehaviour, IEntity {

    [SerializeField] float      m_speed = 4.0f;
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
    [SerializeField] float      jump_modifier_x = 1;
    public float JumpModifierX
    {
        get 
        {
            return jump_modifier_x;
        }
    }
    [SerializeField] float      jump_modifier_y = 1;
    public float JumpModifierY 
    {
        get
        {
            return jump_modifier_y;
        }
    }
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
    private Health playerHealth;
    public float CurrentHealth
    {
        get
        {
            return playerHealth.currentHealth;
        }
    }
    [SerializeField] private LayerMask groundLayer;
    private bool                m_isWallSliding = false;
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


    private HeroState state;
    private List<HeroStates> noMovementStates = new List<HeroStates>() { HeroStates.Roll, HeroStates.Dead };

    // Use this for initialization
    void Start ()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        playerHealth = GetComponent<Health>();
        playerHealth.entity = this;
        m_body2d.gravityScale = gravity;
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
        state = new IdleState();
        state.startState(this);
    }
    void Update()
    {
        if (!CanMove()) 
        {
            state = new IdleState();
            state.startState(this);
            return; // Disable movement if dialog is active
        }
        handleInput();
        m_animator.SetBool("Grounded", isGrounded());
        // -- Handle input and movement --
        m_horizontalInput = Input.GetAxis("Horizontal");
        m_animator.SetFloat("AirSpeedY", m_body2d.linearVelocity.y);
        //Wall Slide
        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        // Swap direction of sprite depending on walk direction
        if (m_horizontalInput > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
        }

        else if (m_horizontalInput < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
        }

        if(!noMovementStates.Contains(state.GetCurrentState()))
        {
            m_body2d.linearVelocity = new Vector2(m_horizontalInput * m_speed, m_body2d.linearVelocity.y);
        }

        //Run
        if (Mathf.Abs(m_horizontalInput) > Mathf.Epsilon)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }
        //Idle
        else
        {
            // Prevents flickering transitions to idle
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }

        m_animator.SetBool("Grounded", isGrounded());
        state.Update();
    }

    void handleInput()
    {
        HeroState newState = state.handleInput();
        if (state.GetCurrentState() != newState.GetCurrentState())
        {
            state = newState;
            state.startState(this);
        }
    }

    public bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.05f, groundLayer);
        return raycastHit.collider != null;
    }

    public bool onWall()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, new Vector2(m_facingDirection, 0), 0.1f, groundLayer);
        if (raycastHit.collider != null)
        {
            m_animator.SetBool("WallSlide", true);
        }
        return raycastHit.collider != null;
    }

    public bool CanMove()
    {
        return !DialogSystem.Instance.DialogActive;
    }

    public void TakeDamage()
    {
        if (!IsBlocking())
        {
            m_animator.SetTrigger("Hurt");
        }
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
