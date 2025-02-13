using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDragManager : MonoBehaviour
{
    public Camera cam;
    public MouseManager grabber;
    public IDraggable grabbedObject;
    public Transform grabbedTransform;
    public bool canDrag;
    public float dragSpeed = 30;

    private Vector3 lastPos;
    private bool isDragging;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if(!isDragging && Input.GetMouseButtonDown(0) && canDrag)
        {
            GameObject clickedOn = grabber.GetHoveredObject();
            if(clickedOn != null)
            {
                IDraggable draggableObject;
                if(clickedOn.transform.root.TryGetComponent(out draggableObject))
                {
                    if (draggableObject.CanDrag())
                    {
                        grabbedObject = draggableObject;
                        grabbedTransform = clickedOn.transform.root;

                        grabbedObject.BeginDrag();

                        lastPos = grabbedTransform.position;
                        isDragging = true;
                    }
                }
            }
        }

        if(isDragging)
        {
            if(grabbedObject != null)
            {
                if(grabbedObject.CanDrag() && canDrag)
                {
                    Vector2 newPos = cam.ScreenToWorldPoint(Input.mousePosition);
                    grabbedTransform.transform.position = Vector3.Lerp(grabbedTransform.transform.position, newPos, Time.deltaTime * dragSpeed);
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

        if(isDragging && Input.GetMouseButtonUp(0))
        {
            if(grabbedObject != null)
            {
                grabbedObject.EndDrag((grabbedTransform.transform.position - lastPos) / Time.deltaTime);
                ClearDragReferences();
            }
        }

        if(grabbedTransform != null)
            lastPos = grabbedTransform.transform.position;
    }

    private void ClearDragReferences()
    {
        grabbedTransform = null;
        grabbedObject = null;
        isDragging = false;
    }
}
