public interface INetworkDrawingService
{
    /// <summary>
    /// Sends a draw-line command over the network.
    /// </summary>
    public bool IsOwner { get; }
    void SendDrawLine(
        UnityEngine.Vector3Int startPoint, 
        UnityEngine.Vector3Int endPoint, 
        float radius, 
        int value, 
        int layer, 
        UnityEngine.Color32 color);
} 