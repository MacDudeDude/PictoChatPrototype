using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine.Tilemaps;

public class PlayerDrawingNetwork : NetworkBehaviour, INetworkDrawingService
{
    // Reference to the pure drawing logic service.
    private PlayerDrawingService drawingService;
    
    // Stores drawing commands for replay to new clients.
    private List<DrawCommand> storedCommands = new List<DrawCommand>();

    public bool IsOwner => base.IsOwner;    

    private void Awake()
    {
        drawingService = GetComponent<PlayerDrawingService>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Assign local client ownership.
        foreach (var clientPair in NetworkManager.ServerManager.Clients)
        {
            if (clientPair.Value.IsLocalClient)
            {
                NetworkObject.GiveOwnership(clientPair.Value);
                break;
            }
        }

        // Request stored commands to replay past drawing actions.
        RequestStoredCommandsServerRpc();
    }

    /// <summary>
    /// Implementation of IRemoteDrawingService.
    /// When the tool calls SendDrawLine, we forward the call via our ServerRpc.
    /// </summary>
    public void SendDrawLine(UnityEngine.Vector3Int startPoint, UnityEngine.Vector3Int endPoint, float radius, int value, int layer, UnityEngine.Color32 color)
    {
        DrawLineServerRpc(startPoint, endPoint, radius, value, layer, color);
    }

    /// <summary>
    /// Sends a drawing command from the client to the server.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void DrawLineServerRpc(UnityEngine.Vector3Int startPoint, UnityEngine.Vector3Int endPoint, float radius, int value, int layer, UnityEngine.Color32 color)
    {
        // Store the command for replaying to future clients.
        DrawCommand command = new DrawCommand
        {
            startPoint = startPoint,
            endPoint = endPoint,
            radius = radius,
            value = value,
            layer = layer,
            color = color
        };
        storedCommands.Add(command);
        
        // Relay the command to all clients.
        DrawLineObserversRpc(startPoint, endPoint, radius, value, layer, color);
    }

    /// <summary>
    /// Observers RPC to update drawing on all clients.
    /// </summary>
    [ObserversRpc]
    public void DrawLineObserversRpc(UnityEngine.Vector3Int startPoint, UnityEngine.Vector3Int endPoint, float radius, int value, int layer, UnityEngine.Color32 color)
    {
        drawingService.DrawLine(startPoint, endPoint, radius, value, layer, color);
    }

    /// <summary>
    /// Server RPC for a client to request stored drawing commands.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestStoredCommandsServerRpc(NetworkConnection sender = null)
    {
        TargetSendStoredCommands(sender, storedCommands.ToArray());
    }

    /// <summary>
    /// Target RPC to send stored drawing commands to a specific client.
    /// </summary>
    [TargetRpc]
    private void TargetSendStoredCommands(NetworkConnection target, DrawCommand[] commands)
    {
        foreach (var cmd in commands)
        {
            drawingService.DrawLine(cmd.startPoint, cmd.endPoint, cmd.radius, cmd.value, cmd.layer, cmd.color);
        }
    }

    /// <summary>
    /// Changes the current artist by their ID
    /// </summary>
    public void ChangeArtist(string artistId)
    {
        ChangeArtistServerRpc(artistId);
    }

    /// <summary>
    /// Server RPC to change the current artist
    /// </summary>
    [ServerRpc]
    private void ChangeArtistServerRpc(string artistId)
    {
        ChangeArtistObserversRpc(artistId);
        NetworkConnection client = SteamPlayerManager.Instance.GetNetworkConnection(ulong.Parse(artistId));
        NetworkObject.GiveOwnership(client);
    }

    /// <summary>
    /// Observers RPC to notify all clients of artist change
    /// </summary>
    [ObserversRpc(RunLocally = true)]
    private void ChangeArtistObserversRpc(string artistId)
    {
        SteamLobbyManager.Instance.ChangeArtist(artistId);
    }

} 