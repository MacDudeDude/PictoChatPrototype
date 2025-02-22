using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System;
using FishNet.Transporting;

/// <summary>
/// Manages synchronized player connection mappings using FishNet SyncDictionaries.
/// This network object is responsible for the registration, removal, and lookup of player connections
/// by storing the mapping between Steam IDs and FishNet network client IDs.
/// </summary>
public class SteamPlayerManager : NetworkBehaviour
{
    public static SteamPlayerManager Instance { get; private set; }

    private readonly SyncDictionary<ulong, int> _steamToConnectionSync = new SyncDictionary<ulong, int>();
    private readonly SyncDictionary<int, ulong> _connectionToSteamSync = new SyncDictionary<int, ulong>();

    // New event that gets fired after a successful registration.
    public event Action<ulong, NetworkConnection> OnPlayerRegistered;

    /// <summary>
    /// Gets all currently connected Steam IDs.
    /// </summary>
    public IEnumerable<ulong> ConnectedSteamIds => _steamToConnectionSync.Keys;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    /// <summary>
    /// Registers a new connection with its Steam ID using the connection's ClientId.
    /// </summary>
    public void RegisterConnection(NetworkConnection connection, ulong steamId)
    {
        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("Attempted to register connection but server is not started");
            return;
        }

        int clientId = connection.ClientId;
        _steamToConnectionSync[steamId] = clientId;
        _connectionToSteamSync[clientId] = steamId;
        Debug.Log($"[SteamPlayerManager] Registered connection - Steam ID: {steamId}, Connection ID: {clientId}");

        // Fire the registration event.
        OnPlayerRegistered?.Invoke(steamId, connection);
    }

    /// <summary>
    /// Removes a connection mapping for the given connection.
    /// </summary>
    public void RemoveConnection(NetworkConnection connection)
    {
        int clientId = connection.ClientId;
        if (_connectionToSteamSync.TryGetValue(clientId, out ulong steamId))
        {
            _steamToConnectionSync.Remove(steamId);
            _connectionToSteamSync.Remove(clientId);
            Debug.Log($"[SteamPlayerManager] Removed connection - Steam ID: {steamId}, Connection ID: {clientId}");
        }
        else
        {
            Debug.LogWarning($"[SteamPlayerManager] Attempted to remove connection {clientId} but no mapping found");
        }
    }

    private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            RemoveConnection(connection);
        }
    }
    /// <summary>
    /// Retrieves the NetworkConnection associated with a given Steam ID.
    /// </summary>
    public NetworkConnection GetNetworkConnection(ulong steamId)
    {
        if (_steamToConnectionSync.TryGetValue(steamId, out int clientId))
        {
            if (InstanceFinder.ServerManager.Clients.TryGetValue(clientId, out NetworkConnection connection))
            {
                Debug.Log($"[SteamPlayerManager] Found NetworkConnection {connection.ClientId} for Steam ID: {steamId}");
                return connection;
            }
            else
            {
                Debug.LogWarning($"[SteamPlayerManager] No NetworkConnection found for Client ID: {clientId} associated with Steam ID: {steamId}");
            }
        }
        else
        {
            Debug.LogWarning($"[SteamPlayerManager] No mapping found for Steam ID: {steamId}");
        }
        return null;
    }

    /// <summary>
    /// Retrieves the Steam ID associated with a given NetworkConnection.
    /// </summary>
    public ulong GetSteamId(NetworkConnection connection)
    {
        int clientId = connection.ClientId;
        if (_connectionToSteamSync.TryGetValue(clientId, out ulong steamId))
        {
            Debug.Log($"[SteamPlayerManager] Found Steam ID {steamId} for Connection ID: {clientId}");
            return steamId;
        }
        else
        {
            Debug.LogWarning($"[SteamPlayerManager] No Steam ID found for connection: {clientId}");
            return 0;
        }
    }

    /// <summary>
    /// Gets the number of players currently connected.
    /// </summary>
    public int GetPlayerCount()
    {
        return _steamToConnectionSync.Count;
    }

    private void OnDestroy()
    {
        InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }
}