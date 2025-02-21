using UnityEngine;
using FishNet.Object;

public interface IDrawingService
{
    MouseManager MouseManager { get; }
    Grid CollisionGrid { get; }
    float PlaceRadius { get; }
    int CurrentLayer { get; }
    Color32 CurrentColor { get; }
    void DrawLine(Vector3Int startPoint, Vector3Int endPoint, float radius, int value, int layer, Color32 color);
} 