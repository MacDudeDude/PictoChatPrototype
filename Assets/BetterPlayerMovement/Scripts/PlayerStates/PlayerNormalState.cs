using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNormalState : PlayerState
{
    [Header("Input")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpBufferDuration = 0.08f;
    [SerializeField] private float groundedBufferDuration = 0.08f;
    [SerializeField] private float groundedSlideBufferDuration = 0.3f;
    [SerializeField] private float jumpCooldownDuration = 0.1f;

    [Header("Movement")]
    [SerializeField] private float m_JumpForce = 400f;                          // Amount of force added when the player jumps.
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;          // Amount of maxSpeed applied to crouching movement. 1 = 100%
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;  // How much to smooth out the movement
    [Range(0, 2f)] [SerializeField] private float m_CrouchedMovementSmoothing = .05f;  // How much to smooth out the movement
    [Range(0, 2f)] [SerializeField] private float m_CrouchedAirMovementSmoothing = .05f;  // How much to smooth out the movement
    [SerializeField] private bool m_AirControl = false;                         // Whether or not a player can steer while jumping;
    
    [Header("Collision")]
    [SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
    [SerializeField] private Transform m_GroundCheck;                           // A position marking where to check if the player is grounded.
    [SerializeField] private Transform m_CeilingCheck;                          // A position marking where to check for ceilings
    [SerializeField] private Collider2D m_CrouchDisableCollider;                // A collider that will be disabled when crouching
    [SerializeField] private float k_GroundedRadius = .1f; // Radius of the overlap circle to determine if grounded
    [SerializeField] private float k_CeilingRadius = .1f; // Radius of the overlap circle to determine if the player can stand up

    //Misc Values
    private bool m_wasGrounded;
    private bool m_Grounded;            // Whether or not the player is grounded.
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private Vector3 velocity = Vector3.zero;
    private bool m_Crouched;

    //Move Values
    private float horizontalMove;
    private float verticalMove;
    private float jumpBufferTime;
    private float groundedBufferTime;
    private float groundedSlideBufferTime;
    private bool doJump;
    private float jumpCooldownTime;

    public override void Init(Player player, PlayerStateMachine playerStateMachine)
    {
        base.Init(player, playerStateMachine);
        m_Rigidbody2D = rb;
    }

    public override void EnterState()
    {
        base.EnterState();
        player.animator.SetLayerWeight(0, 1);
    }

    public override void ExitState()
    {
        base.ExitState();
        player.animator.SetLayerWeight(0, 0);
    }

    public override void FrameUpdate()
    {
        GetInputs();
        SetAnimatorValues();
    }

    public override void PhysicsUpdate()
    {
        GroundCheck();
        Move();
    }

    private void SetAnimatorValues()
    {
        player.animator.SetFloat("HorizontalVelocity", Mathf.Abs(m_Rigidbody2D.velocity.x));
        player.animator.SetFloat("VerticalVelocity", m_Rigidbody2D.velocity.y);
        player.animator.SetBool("Crouching", m_Crouched);
        player.animator.SetBool("Jumping", jumpCooldownTime > 0);

        bool falling = m_Rigidbody2D.velocity.y < 0 && !m_Grounded;
        if (m_Crouched && groundedSlideBufferTime > 0)
            falling = false;

        player.animator.SetBool("Falling", falling);
    }

    private void GetInputs()
    {
        horizontalMove = 0;
        verticalMove = 0;

        horizontalMove = Input.GetAxisRaw("Horizontal") * moveSpeed;
        verticalMove = Input.GetAxisRaw("Vertical");

        jumpBufferTime -= Time.deltaTime;
        jumpCooldownTime -= Time.deltaTime;
        groundedBufferTime -= Time.deltaTime;
        groundedSlideBufferTime -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && jumpCooldownTime < 0)
        {
            jumpBufferTime = jumpBufferDuration;
        }

        if (jumpBufferTime > 0)
        {
            if (groundedBufferTime > 0)
            {
                groundedBufferTime = 0;
                groundedSlideBufferTime = 0;
                jumpBufferTime = 0;

                doJump = true;
            }
        }
    }

    public void Move()
    {
        bool crouch = verticalMove < 0;
        if (crouch && !m_Crouched && groundedBufferTime < 0)
            crouch = false;

        horizontalMove *= Time.fixedDeltaTime;

        // If crouching, check to see if the character can stand up
        if (!crouch && m_Crouched)
        {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }

        if((crouch && !m_Crouched && groundedBufferTime > 0))
        {
            velocity.x = 0;
            //m_Rigidbody2D.velocity = new Vector2(0, m_Rigidbody2D.velocity.y);
        }

        if(crouch && !m_Crouched)
        {
            StartCoroutine(JumpSqueeze(1.2f, 0.9f, 0.05f));
        }else if(!crouch && m_Crouched)
        {
            StartCoroutine(JumpSqueeze(0.8f, 1.1f, 0.05f));
        }

        if(m_Grounded && !m_wasGrounded)
        {
            StartCoroutine(JumpSqueeze(1.25f, 0.8f, 0.05f));
        }

        //only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {
            // If crouching
            if (crouch)
            {
                // Reduce the speed by the crouchSpeed multiplier
                m_Crouched = true;
                horizontalMove *= (groundedSlideBufferTime < 0) ? m_CrouchSpeed : 0;

                // Disable one of the colliders when crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.gameObject.SetActive(false);
            }
            else
            {
                m_Crouched = false;
                // Enable the collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.gameObject.SetActive(true);
            }

            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(horizontalMove * 10f, m_Rigidbody2D.velocity.y);
            // And then smoothing it out and applying it to the character

            float movementSmoothing = (crouch) ? m_CrouchedMovementSmoothing : m_MovementSmoothing;
            if (groundedSlideBufferTime <= 0 && crouch)
                movementSmoothing = m_CrouchedAirMovementSmoothing;

            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref velocity, movementSmoothing);

            // If the input is moving the player right and the player is facing left...
            if (horizontalMove > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (horizontalMove < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }

        // If the player should jump...
        if (doJump)
        {
            StartCoroutine(JumpSqueeze(0.75f, 1.25f, 0.07f));

            // Add a vertical force to the player.
            m_Grounded = false;
            m_Rigidbody2D.velocity = (new Vector2(m_Rigidbody2D.velocity.x, m_JumpForce));

            doJump = false;
            jumpCooldownTime = jumpCooldownDuration;
        }

        m_wasGrounded = m_Grounded;
    }

    public void GroundCheck()
    {
        m_Grounded = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                groundedBufferTime = groundedBufferDuration;
                groundedSlideBufferTime = groundedSlideBufferDuration;
                m_Grounded = true;
                break;
            }
        }
    }

    IEnumerator JumpSqueeze(float xSqueeze, float ySqueeze, float seconds)
    {
        Vector3 originalSize = Vector3.one;
        Vector3 newSize = new Vector3(xSqueeze, ySqueeze, originalSize.z);
        float t = 0f;
        while (t <= 1.0)
        {
            t += Time.deltaTime / seconds;
            player.spritesHolder.localScale = Vector3.Lerp(originalSize, newSize, t);
            yield return null;
        }
        t = 0f;
        while (t <= 1.0)
        {
            t += Time.deltaTime / seconds;
            player.spritesHolder.localScale = Vector3.Lerp(newSize, originalSize, t);
            yield return null;
        }

    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(m_GroundCheck.position, k_GroundedRadius);
        Gizmos.DrawWireSphere(m_CeilingCheck.position, k_CeilingRadius);
    }
}
