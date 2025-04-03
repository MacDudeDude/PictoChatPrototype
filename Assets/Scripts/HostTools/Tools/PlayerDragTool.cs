using UnityEngine;

public class PlayerDragTool : DrawingToolBase
{
    public float dragSpeed = 30f;
    public float throwForceMultiplier = 10f;
    public int velocityBufferSize = 5;

    private IDraggable grabbedObject;
    private Transform grabbedTransform;
    private Vector3 lastPos;
    private Vector3 dragVelocity;
    private bool isDragging;
    private bool canDrag;
    private float dragUpdateRate = 0.01f;
    private float lastDragUpdate;
    private Vector3[] velocityBuffer;
    private int velocityBufferIndex;
    private float lastFixedTime;

    public void Awake()
    {
        canDrag = true;
        velocityBuffer = new Vector3[velocityBufferSize];
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
                    Vector3 currentVelocity = (targetPos - (Vector2)lastPos) / dragUpdateRate;
                    velocityBuffer[velocityBufferIndex] = currentVelocity;
                    velocityBufferIndex = (velocityBufferIndex + 1) % velocityBufferSize;

                    lastPos = grabbedTransform.position;
                    lastDragUpdate = Time.time;

                    Debug.Log("[PlayerDragTool] Update drag velocity: " + currentVelocity);
                    grabbedObject.UpdateDragPosition(targetPos);
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
            Vector3 averageVelocity = Vector3.zero;
            foreach (Vector3 vel in velocityBuffer)
            {
                averageVelocity += vel;
                Debug.Log("[PlayerDragTool] Velocity: " + vel);
            }
            averageVelocity /= velocityBufferSize;
            Debug.Log("[PlayerDragTool] Average velocity: " + averageVelocity);

            Vector3 throwVelocity = averageVelocity * throwForceMultiplier;

            float maxThrowSpeed = 20f;
            if (throwVelocity.magnitude > maxThrowSpeed)
            {
                throwVelocity = throwVelocity.normalized * maxThrowSpeed;
            }

            Debug.Log("[PlayerDragTool] Throw velocity: " + throwVelocity);

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
        for (int i = 0; i < velocityBufferSize; i++)
        {
            velocityBuffer[i] = Vector3.zero;
        }
        velocityBufferIndex = 0;
    }
}