using UnityEngine;

public class PlayerDragTool : DrawingToolBase
{
    public float dragSpeed = 30f;
    public float throwForceMultiplier = 1f;

    private IDraggable grabbedObject;
    private Transform grabbedTransform;
    private Vector3 lastPos;
    private Vector3 dragVelocity;
    private bool isDragging;
    private bool canDrag;
    private float dragUpdateRate = 0.01f;
    private float lastDragUpdate;

    public void Awake()
    {
        canDrag = true;
    }

    public override void OnToolUpdate()
    {
        if (!CanUse()) return;
        HandleDragging();
    }

    private void HandleDragging()
    {
        HandleDragStart();
        HandleDragUpdate();
        HandleDragEnd();

        if (grabbedTransform != null)
            lastPos = grabbedTransform.position;
    }

    private void HandleDragStart()
    {
        if (!isDragging && Input.GetMouseButtonDown(0) && canDrag)
        {
            GameObject clickedOn = drawingService.MouseManager.GetHoveredObject();
            if (clickedOn != null && clickedOn.transform.root.TryGetComponent(out IDraggable draggableObject) == true && draggableObject.CanDrag())
            {
                grabbedObject = draggableObject;
                grabbedTransform = clickedOn.transform.root;
                grabbedObject.BeginDrag();
                lastPos = grabbedTransform.position;
                isDragging = true;
            }
        }
    }

    private void HandleDragUpdate()
    {
        if (isDragging && grabbedObject != null)
        {
            if (grabbedObject.CanDrag() && canDrag)
            {
                Vector2 targetPos = GetCurrentMousePosition();

                if (Time.time - lastDragUpdate >= dragUpdateRate)
                {
                    dragVelocity = (targetPos - (Vector2)lastPos) / dragUpdateRate;
                    lastPos = grabbedTransform.position;
                    lastDragUpdate = Time.time;

                    if (grabbedObject is Player player)
                    {
                        player.UpdateDragPosition(targetPos);
                    }
                }
            }
            else
            {
                ClearDragReferences();
            }
        }
    }

    private void HandleDragEnd()
    {
        if (isDragging && Input.GetMouseButtonUp(0) && grabbedObject != null)
        {
            Vector3 throwVelocity = dragVelocity * throwForceMultiplier;
            grabbedObject.EndDrag(throwVelocity);
            ClearDragReferences();
        }
    }

    public override void OnToolSelected()
    {
        base.OnToolSelected();
        canDrag = true;
    }

    public override void OnToolDeselected()
    {
        base.OnToolDeselected();
        canDrag = false;
        if (isDragging)
        {
            grabbedObject?.EndDrag(Vector3.zero);
            ClearDragReferences();
        }
    }

    private void ClearDragReferences()
    {
        grabbedTransform = null;
        grabbedObject = null;
        isDragging = false;
    }
}