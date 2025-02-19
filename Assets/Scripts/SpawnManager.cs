using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using FishNet;
using FishNet.Connection;
using FishNet.Object;

public class SpawnManager : MonoBehaviour
{

    [SerializeField]
    private NetworkObject prespawnedPlayer;
    [SerializeField]
    private NetworkObject playerPrefab;
    [SerializeField]
    private NetworkObject artistPrefab;

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
    private void SpawnPlayer(ulong steamId, NetworkConnection conn)
    {
        ulong artistId = ulong.Parse(SteamLobbyManager.Instance.GetArtist());
        NetworkConnection artistConn = SteamPlayerManager.Instance.GetNetworkConnection(artistId);
        if (conn == artistConn)
        {
            Debug.Log("[SpawnManager] Spawning artist with owner: " + conn);
            GameObject artistInstance = Instantiate(artistPrefab.gameObject);
            InstanceFinder.ServerManager.Spawn(artistInstance, conn);
        }
        else
        {
            Debug.Log("[SpawnManager] Spawning player with owner: " + conn);
            GameObject playerInstance = Instantiate(playerPrefab.gameObject);
            InstanceFinder.ServerManager.Spawn(playerInstance, conn);
        }
    }

    private void RespawnPlayers(string newArtistId = null)
    {
        ulong artistId = 0;
        NetworkConnection artistConn = null;
        if (newArtistId == null)
        {
            artistId = ulong.Parse(SteamLobbyManager.Instance.GetArtist());
            artistConn = SteamPlayerManager.Instance.GetNetworkConnection(artistId);
        }
        else
        {
            artistId = ulong.Parse(newArtistId);
            artistConn = SteamPlayerManager.Instance.GetNetworkConnection(artistId);
        }

        foreach (var conn in InstanceFinder.ServerManager.Clients)
        {
            if (conn.Value == artistConn)
            {
                Debug.Log("[SpawnManager] Respawning artist with owner: " + conn.Value);
                GameObject artistInstance = Instantiate(artistPrefab.gameObject);
                InstanceFinder.ServerManager.Spawn(artistInstance, conn.Value);
            }
            else
            {
                Debug.Log("[SpawnManager] Respawning player with owner: " + conn.Value);
                GameObject playerInstance = Instantiate(playerPrefab.gameObject);
                InstanceFinder.ServerManager.Spawn(playerInstance, conn.Value);
            }
        }
    }

}
