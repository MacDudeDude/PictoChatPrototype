using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boulder : MonoBehaviour, IDraggable
{
    public Rigidbody2D rb;
    public LayerMask canKillLayers; // For performance reasons (won't try get componenent every single collision)
    public Vector2 lastVelocity;
    private float killCollisionSpeed;

    public void BeginDrag()
    {
        rb.simulated = false;
    }

    public bool CanDrag()
    {
        return true;
    }

    public void EndDrag(Vector3 dragEndVelocity)
    {
        rb.simulated = true;
        rb.velocity = dragEndVelocity;
    }

    public void FixedUpdate()
    {
        lastVelocity = rb.velocity;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(canKillLayers == (canKillLayers | (1 << collision.gameObject.layer)))
        {
            Debug.Log(lastVelocity.magnitude);
            if(lastVelocity.magnitude > killCollisionSpeed)
            {
                IKillable killable;
                if(collision.transform.root.TryGetComponent(out killable))
                {
                    killable.Kill();
                }
            }
        }
    }
}
