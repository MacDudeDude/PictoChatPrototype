using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PlayerDragTool : ToolState
{
    public MouseManager grabber;
    public IDraggable grabbedObject;
    public Transform grabbedTransform;
    public bool canDrag;
    public float dragSpeed = 30;
    public Camera cam;

    private Vector3 lastPos;
    private bool isDragging;

    public void DragToolUpdate()
    {
        if (!isDragging && Input.GetMouseButtonDown(0) && canDrag)
        {
            GameObject clickedOn = grabber.GetHoveredObject();
            if (clickedOn != null)
            {
                if (clickedOn.transform.root.TryGetComponent(out Player player))
                {
                    if (player.CanDrag())
                    {

                        grabbedObject = player;
                        grabbedTransform = clickedOn.transform.root;

                        grabbedObject.BeginDrag();
                        lastPos = grabbedTransform.position;
                        isDragging = true;
                    }
                }
            }
        }

        if (isDragging)
        {
            if (grabbedObject != null)
            {
                if (grabbedObject.CanDrag() && canDrag)
                {
                    Vector2 newPos = cam.ScreenToWorldPoint(Input.mousePosition);

                    if (grabbedObject is Player player)
                    {
                        Vector3 blendedPos = Vector3.Lerp(grabbedTransform.position, newPos, Time.deltaTime * dragSpeed);
                        player.DragUpdateServerRpc(blendedPos, Time.deltaTime);
                    }
                }
                else
                {
                    ClearDragReferences();
                }
            }
            else
            {
                ClearDragReferences();
            }
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (grabbedObject != null)
            {
                Vector3 dragVelocity = (grabbedTransform.position - lastPos) / Time.deltaTime;
                grabbedObject.EndDrag(dragVelocity);
                ClearDragReferences();
            }
        }

        if (grabbedTransform != null)
            lastPos = grabbedTransform.position;
    }

    private void ClearDragReferences()
    {
        grabbedTransform = null;
        grabbedObject = null;
        isDragging = false;
    }
}
