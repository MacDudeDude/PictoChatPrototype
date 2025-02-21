using UnityEngine;

public class PlayerDragTool : DrawingToolBase
{
    public float dragSpeed = 30;

    private IDraggable grabbedObject;
    private Transform grabbedTransform;
    private Vector3 lastPos;
    private bool isDragging;
    private bool canDrag;

    public  void Awake()
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
                Vector2 newPos = GetCurrentMousePosition();
                grabbedTransform.position = Vector3.Lerp(grabbedTransform.position, newPos, Time.deltaTime * dragSpeed);
            }
            else
            {
                ClearDragReferences();
            }
        }
        else if (isDragging)
        {
            ClearDragReferences();
        }
    }

    private void HandleDragEnd()
    {
        if (isDragging && Input.GetMouseButtonUp(0) && grabbedObject != null)
        {
            grabbedObject.EndDrag((grabbedTransform.position - lastPos) / Time.deltaTime);
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