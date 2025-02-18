using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTopDownState : PlayerState
{
    public float moveSpeed;
    public float moveSpeedStrength = 10f;
    public float movementSmoothing;

    private bool m_FacingRight = true;

    private Vector2 moveInput;
    private Vector3 velocity;

    public override void Init(Player player, PlayerStateMachine playerStateMachine)
    {
        base.Init(player, playerStateMachine);
    }

    public override void EnterState()
    {
        base.EnterState();
        rb.gravityScale = 0;
    }

    public override void ExitState()
    {
        base.ExitState();
        transform.rotation = Quaternion.identity;
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        GetInput();
    }


    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        Move();
    }

    public void GetInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        moveInput.Normalize();
        moveInput *= moveSpeed;
    }

    public void Move()
    {
        Vector3 targetVelocity = moveInput * moveSpeedStrength * Time.fixedDeltaTime;
        rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref velocity, movementSmoothing);

        if (moveInput.x > 0.05f && !m_FacingRight) {
            Flip();
            StartCoroutine(JumpSqueeze(1.25f, 0.8f, 0.05f));
        } else if (moveInput.x < -0.05f && m_FacingRight) {
            Flip();
            StartCoroutine(JumpSqueeze(1.25f, 0.8f, 0.05f));
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, rb.velocity), 15 * Time.fixedDeltaTime);
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
}
