using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
public class SpawnManager : MonoBehaviour
{

    [SerializeField]
    private NetworkObject prespawnedPlayer;
    [SerializeField]
    private NetworkObject playerPrefab;
    private PlayerDrawingNetwork artist;

    private Dictionary<NetworkConnection, GameObject> players = new Dictionary<NetworkConnection, GameObject>();

    private void Start()
    {
        Initialize();

        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnArtistChanged += OnArtistChanged;
        }

        // Subscribe to the registration event.
        if (SteamPlayerManager.Instance != null)
        {
            SteamPlayerManager.Instance.OnPlayerRegistered += OnPlayerRegistered;
        }

        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    private void OnDestroy()
    {
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnArtistChanged -= OnArtistChanged;
        }

        if (SteamPlayerManager.Instance != null)
        {
            SteamPlayerManager.Instance.OnPlayerRegistered -= OnPlayerRegistered;
        }
    }


    private void Initialize()
    {
        InstanceFinder.NetworkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
    }
    public void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer)
            return;
        if (prespawnedPlayer == null)
        {
            Debug.LogError("[SpawnManager] Prespawned player prefab is not set");
            return;
        }
        GameObject prespawnedPlayerInstance = Instantiate(prespawnedPlayer.gameObject);
        Debug.Log("[SpawnManager] Pre-spawning player with owner: " + conn);
        InstanceFinder.ServerManager.Spawn(prespawnedPlayerInstance, conn);
    }

    private void OnArtistChanged(string newArtistId)
    {
        RespawnPlayers(newArtistId);
    }

    // This method is called when a player has been registered.
    private void OnPlayerRegistered(ulong steamId, NetworkConnection conn)
    {
        SpawnPlayer(steamId, conn);
    }
    private void SpawnPlayer(ulong steamId = 0, NetworkConnection conn = null)
    {
        ulong artistId = SteamLobbyManager.Instance.GetArtist();
        NetworkConnection artistConn = SteamPlayerManager.Instance.GetNetworkConnection(artistId);
        //if (conn != artistConn)
        //{
        //    Debug.Log("[SpawnManager] Spawning player with owner: " + conn);
        //    GameObject playerInstance = Instantiate(playerPrefab.gameObject);
        //    InstanceFinder.ServerManager.Spawn(playerInstance, conn);
        //    players.Add(conn, playerInstance);
        //}

        Debug.Log("[SpawnManager] Spawning player with owner: " + conn);
            GameObject playerInstance = Instantiate(playerPrefab.gameObject);
            InstanceFinder.ServerManager.Spawn(playerInstance, conn);
            players.Add(conn, playerInstance);
    }

    private void RespawnPlayers(string newArtistId = null)
    {
        ulong artistId = 0;
        NetworkConnection artistConn = null;
        if (newArtistId == null)
        {
            artistId = SteamLobbyManager.Instance.GetArtist();
            artistConn = SteamPlayerManager.Instance.GetNetworkConnection(artistId);
        }
        else
        {
            artistId = ulong.Parse(newArtistId);
            artistConn = SteamPlayerManager.Instance.GetNetworkConnection(artistId);
        }

        DespawnPlayers();
        foreach (var conn in InstanceFinder.ServerManager.Clients)
        {
            SpawnPlayer(0, conn.Value);
        }
    }
    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            InstanceFinder.ServerManager.Despawn(players[conn]);
            players.Remove(conn);
        }
    }
    private void DespawnPlayers()
    {
        foreach (var player in players)
        {
            InstanceFinder.ServerManager.Despawn(player.Value);
        }
    }

}
