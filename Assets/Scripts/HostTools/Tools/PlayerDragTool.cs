using UnityEngine;

public class PlayerDragTool : DrawingToolBase
{
    public float dragSpeed = 30f;
    public float throwForceMultiplier = 10f;
    public int velocityBufferSize = 5;
    public float maxThrowSpeed = 5f;

    private IDraggable grabbedObject;
    private Transform grabbedTransform;
    private Vector3 lastPos;
    private bool isDragging;
    private bool canDrag;
    private Vector3[] velocityBuffer;
    private int velocityBufferIndex;

    public void Awake()
    {
        canDrag = true;
        velocityBuffer = new Vector3[velocityBufferSize];
        Debug.Log("[PlayerDragTool] Initialized with buffer size: " + velocityBufferSize);
    }

    public override void OnToolUpdate()
    {
        if (!CanUse())
        {
            Debug.Log("[PlayerDragTool] Cannot use tool");
            return;
        }
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
            Debug.Log("[PlayerDragTool] Attempting to start drag");
            GameObject clickedOn = drawingService.MouseManager.GetHoveredObject();
            if (clickedOn != null)
            {
                Debug.Log("[PlayerDragTool] Clicked on: " + clickedOn.name);
                if (clickedOn.transform.root.TryGetComponent(out IDraggable draggableObject) == true)
                {
                    Debug.Log("[PlayerDragTool] Found draggable object");
                    if (draggableObject.CanDrag())
                    {
                        Debug.Log("[PlayerDragTool] Object can be dragged, starting drag");
                        grabbedObject = draggableObject;
                        grabbedTransform = clickedOn.transform.root;
                        grabbedObject.BeginDrag();
                        lastPos = grabbedTransform.position;
                        isDragging = true;
                    }
                    else
                    {
                        Debug.Log("[PlayerDragTool] Object cannot be dragged at this time");
                    }
                }
                else
                {
                    Debug.Log("[PlayerDragTool] Object is not draggable");
                }
            }
            else
            {
                Debug.Log("[PlayerDragTool] No object clicked");
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
                Debug.Log("[PlayerDragTool] Dragging to position: " + targetPos);

                // Calculate velocity even if not updating position
                Vector3 currentVelocity = (targetPos - (Vector2)lastPos) / Time.deltaTime;
                velocityBuffer[velocityBufferIndex] = currentVelocity;
                velocityBufferIndex = (velocityBufferIndex + 1) % velocityBufferSize;
                Debug.Log("[PlayerDragTool] Current velocity: " + currentVelocity + ", Buffer index: " + velocityBufferIndex);

                grabbedObject.UpdateDragPosition(targetPos);
            }
            else
            {
                Debug.Log("[PlayerDragTool] Can no longer drag object, clearing references");
                ClearDragReferences();
            }
        }
    }

    private void HandleDragEnd()
    {
        if (isDragging && Input.GetMouseButtonUp(0) && grabbedObject != null)
        {
            Debug.Log("[PlayerDragTool] Ending drag, calculating throw velocity");
            Vector3 averageVelocity = Vector3.zero;
            foreach (Vector3 vel in velocityBuffer)
            {
                averageVelocity += vel;
            }
            averageVelocity /= velocityBufferSize;
            Debug.Log("[PlayerDragTool] Average velocity: " + averageVelocity);
            Vector3 throwVelocity = averageVelocity * throwForceMultiplier;
            Debug.Log("[PlayerDragTool] Initial throw velocity: " + throwVelocity + " (multiplier: " + throwForceMultiplier + ")");

            if (throwVelocity.magnitude > maxThrowSpeed)
            {
                throwVelocity = throwVelocity.normalized * maxThrowSpeed;
                Debug.Log("[PlayerDragTool] Clamped velocity: " + throwVelocity + " (max speed: " + maxThrowSpeed + ")");
            }

            Debug.Log("[PlayerDragTool] Ending drag with final velocity: " + throwVelocity);
            grabbedObject.EndDrag(throwVelocity);
            ClearDragReferences(); // Clear references after using the buffer
        }
    }

    public override void OnToolSelected()
    {
        base.OnToolSelected();
        canDrag = true;
        Debug.Log("[PlayerDragTool] Tool selected, dragging enabled");
    }

    public override void OnToolDeselected()
    {
        base.OnToolDeselected();
        Debug.Log("[PlayerDragTool] Tool deselected, dragging disabled");
        canDrag = false;
        if (isDragging)
        {
            Debug.Log("[PlayerDragTool] Ending drag due to tool deselection");
            grabbedObject?.EndDrag(Vector3.zero);
            ClearDragReferences();
        }
    }

    private void ClearDragReferences()
    {
        Debug.Log("[PlayerDragTool] Clearing drag references");
        grabbedTransform = null;
        grabbedObject = null;
        isDragging = false;
        for (int i = 0; i < velocityBufferSize; i++)
        {
            velocityBuffer[i] = Vector3.zero;
        }
        velocityBufferIndex = 0;
    }
}