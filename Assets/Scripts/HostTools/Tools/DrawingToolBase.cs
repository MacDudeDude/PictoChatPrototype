using UnityEngine;

public abstract class DrawingToolBase : MonoBehaviour, IDrawingTool
{
    protected IDrawingService drawingService;
    protected INetworkDrawingService remoteDrawingService;
    protected Camera mainCamera;
    protected Vector2 lastMousePosition;
    public void Initialize(IDrawingService service, INetworkDrawingService networkService)
    {
        drawingService = service;
        remoteDrawingService = networkService;
        mainCamera = Camera.main;
    }

    public virtual void OnToolSelected() 
    {
        lastMousePosition = GetCurrentMousePosition();
    }

    public virtual void OnToolDeselected() { }

    public virtual bool CanUse()
    {
        return remoteDrawingService.IsOwner;
    }

    public abstract void OnToolUpdate();

    protected Vector2 GetCurrentMousePosition()
    {
        return mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    protected void HandleDrawing(int value)
    {
        if (!CanUse()) return;

        if (Input.GetMouseButtonDown(0))
            lastMousePosition = GetCurrentMousePosition();

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = GetCurrentMousePosition();

            if (drawingService.MouseManager.GetObjectBetweenTwoPoints(mousePos, lastMousePosition) == null)
            {
                Vector3Int gridStartpoint = drawingService.CollisionGrid.WorldToCell(lastMousePosition);
                Vector3Int gridEndpoint = drawingService.CollisionGrid.WorldToCell(mousePos);
                remoteDrawingService.SendDrawLine(gridStartpoint, gridEndpoint, drawingService.PlaceRadius, value, drawingService.CurrentLayer, value == 0 ? Color.clear : drawingService.CurrentColor);
            }

            lastMousePosition = mousePos;
        }
    }
} 