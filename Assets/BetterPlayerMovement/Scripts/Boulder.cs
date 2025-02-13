using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boulder : MonoBehaviour, IDraggable
{
    public Rigidbody2D rb;
    public float killCollisionSpeed;
    public LayerMask canKillLayers; // For performance reasons (won't try get componenent every single collision)

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

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(canKillLayers == (canKillLayers | (1 << collision.gameObject.layer)))
        {
            if(collision.relativeVelocity.magnitude > killCollisionSpeed)
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
