using UnityEngine;
using System.Collections;
using Assets.Scripts;

public class HeroKnight : MonoBehaviour, IEntity {

    [SerializeField] float      m_speed = 4.0f;
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] float      gravity = 1;
    [SerializeField] float      jump_modifier_x = 1;
    [SerializeField] float      jump_modifier_y = 1;
    [SerializeField] bool       m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroKnight   m_wallSensorR1;
    private Sensor_HeroKnight   m_wallSensorR2;
    private Sensor_HeroKnight   m_wallSensorL1;
    private Sensor_HeroKnight   m_wallSensorL2;
    private BoxCollider2D boxCollider;
    private Health playerHealth;
    [SerializeField] private LayerMask groundLayer;
    private bool                m_isWallSliding = false;
    private bool                m_rolling = false;
    private int                 m_facingDirection = 1;
    private int                 m_currentAttack = 0;
    private float               m_timeSinceAttack = 0.0f;
    private float               m_delayToIdle = 0.0f;
    private float               m_wallCooldown = 0.0f;
    private float               m_rollDuration = 8.0f / 14.0f;
    private float               m_rollCurrentTime;
    private float               m_horizontalInput;
    private bool m_isBlocking = false;


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
    }

    // Update is called once per frame
    void Update ()
    {
        m_animator.SetBool("Grounded", isGrounded());
        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;

        // Increase timer that checks roll duration
        if (m_rolling)
        {
            m_rollCurrentTime += Time.deltaTime;
        }

        // Disable rolling if timer extends duration
        if(m_rollCurrentTime > m_rollDuration)
        {
             m_rolling = false;
        }

        m_animator.SetBool("Grounded", isGrounded());

        // -- Handle input and movement --
        m_horizontalInput = Input.GetAxis("Horizontal");

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
        
        // Move
        if (!m_rolling)
        {
            m_body2d.linearVelocity = new Vector2(m_horizontalInput * m_speed, m_body2d.linearVelocity.y);
        }

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.linearVelocity.y);

        // -- Handle Animations --
        //Wall Slide
        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        

        //Attack
        if(IsAttacking())
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

        // Block
        else if (Input.GetMouseButtonDown(1) && !m_rolling)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true); 
            m_isBlocking = true;
        }

        else if (Input.GetMouseButtonUp(1))
        {
            m_animator.SetBool("IdleBlock", false);
            m_isBlocking = false;
        }

        // Roll
        else if (Input.GetKeyDown("left shift") && !m_rolling && !onWall())
        {
            m_rolling = true;
            m_animator.SetTrigger("Roll");
            m_body2d.gravityScale = 0.1f;
            m_body2d.linearVelocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.linearVelocity.y + 2);
            m_body2d.gravityScale = gravity;
        }

        //Jump
        else if (Input.GetKeyDown("space") && !m_rolling)
        {
            if (m_wallCooldown > 0.5f)
            {
                if (onWall() && !isGrounded() && !m_rolling)
                {
                    m_body2d.gravityScale = 0;
                    m_body2d.linearVelocity = Vector2.zero;
                }
                else
                {
                    m_body2d.gravityScale = gravity;
                }

                Jump();
            }
        }
        //Run
        else if (Mathf.Abs(m_horizontalInput) > Mathf.Epsilon)
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
                if(m_delayToIdle < 0)
                    m_animator.SetInteger("AnimState", 0);
        }


        if (m_wallCooldown < 3f)
        {
            m_wallCooldown += Time.deltaTime;
        }
        m_animator.SetBool("Grounded", isGrounded());
    }

    private void Jump()
    {
        m_animator.SetBool("WallSlide", false);
        m_body2d.gravityScale = gravity;
        if (isGrounded())
        {
            m_animator.SetTrigger("Jump");
            m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }
        else if (onWall() && !isGrounded())
        {
            if (m_horizontalInput == 0)
            {
                m_body2d.linearVelocity = new Vector2(-Mathf.Sign(m_facingDirection) * 6, 0);
            }
            else
            {
                m_animator.SetTrigger("Jump");
                m_body2d.gravityScale = 5f;
                m_body2d.linearVelocity = new Vector2(-Mathf.Sign(m_facingDirection) * jump_modifier_x, jump_modifier_y);
            }
            m_animator.SetBool("WallSlide", false);
            m_body2d.gravityScale = gravity;
            m_wallCooldown = 0;
        }
    }

    private bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.05f, groundLayer);
        return raycastHit.collider != null;
    }

    private bool onWall()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, new Vector2(m_facingDirection, 0), 0.1f, groundLayer);
        if (raycastHit.collider != null)
        {
            m_animator.SetBool("WallSlide", true);
        }
        return raycastHit.collider != null;
    }

    public float TakeDamage(float _damage)
    {
        if (!IsBlocking())
        {
            m_animator.SetTrigger("Hurt");
            return _damage;
        }
        return 0;
    }

    public void Die()
    {
        m_animator.SetBool("noBlood", m_noBlood);
        m_animator.SetTrigger("Death");
    }

    public bool IsBlocking()
    {
        return m_isBlocking;
    }

    public bool IsAttacking()
    {
        return Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling;
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
