using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boulder : MonoBehaviour, IDraggable
{
    public Rigidbody2D rb;
    public LayerMask canKillLayers; // For performance reasons (won't try get componenent every single collision)
    public float killCollisionSpeed;
    private Vector2 lastVelocity;

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
    public void UpdateDragPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (canKillLayers == (canKillLayers | (1 << collision.gameObject.layer)))
        {
            if (lastVelocity.magnitude > killCollisionSpeed)
            {
                IKillable killable;
                if (collision.transform.root.TryGetComponent(out killable))
                {
                    killable.Kill();
                }
            }
        }
    }
}
