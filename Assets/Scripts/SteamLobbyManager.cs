using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using FishNet.Object;
using System.Collections.Generic;

public class SteamLobbyManager : NetworkBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }
    [SerializeField]
    private int maxPlayers = 4;
    public Lobby? CurrentLobby { get; private set; }

    // List for connected SteamIds.
    public List<SteamId> ConnectedPlayers { get; private set; } = new List<SteamId>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Process Steam callbacks every frame.
    void Update()
    {
        SteamClient.RunCallbacks();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _ = CreateLobby();
    }

    private async Task CreateLobby()
    {
        if (!IsServerInitialized)
            return;

        // Create the lobby and await the result.
        var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (lobbyResult.HasValue)
        {
            CurrentLobby = lobbyResult.Value;
            Debug.Log("Lobby created with ID: " + CurrentLobby?.Id);
            // Add the host to the connected players list.
            ConnectedPlayers.Add(SteamClient.SteamId);

            // Subscribe to the global lobby callbacks.
            SteamMatchmaking.OnLobbyMemberJoined += OnMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += OnMemberLeave;
            Debug.Log("Registered lobby callbacks");
        }
        else
        {
            Debug.LogError("Failed to create lobby!");
        }
    }

    // Global callback when a member joins a lobby.
    private void OnMemberJoined(Lobby lobby, Friend friend)
    {
        // Check to make sure this event is for our lobby.
        if (CurrentLobby == null || lobby.Id != CurrentLobby.Value.Id)
            return;

        Debug.Log("Member joined: " + friend.Id);
        if (IsServerInitialized)
        {
            ConnectedPlayers.Add(friend.Id);
            UpdatePlayerListClientRpc(ConnectedPlayers.ToArray());
        }
    }

    // Global callback when a member leaves a lobby.
    private void OnMemberLeave(Lobby lobby, Friend friend)
    {
        if (CurrentLobby == null || lobby.Id != CurrentLobby.Value.Id)
            return;

        Debug.Log("Member left: " + friend.Id);
        if (IsServerInitialized)
        {
            ConnectedPlayers.Remove(friend.Id);
            UpdatePlayerListClientRpc(ConnectedPlayers.ToArray());
        }
    }

    [ServerRpc]
    public void UpdatePlayerListServerRpc(SteamId[] players)
    {
        ConnectedPlayers = new List<SteamId>(players);
        UpdatePlayerListClientRpc(players);
    }

    [ObserversRpc]
    private void UpdatePlayerListClientRpc(SteamId[] players)
    {
        ConnectedPlayers = new List<SteamId>(players);
    }

    private void OnDestroy()
    {
        // Unsubscribe from the global events.
        SteamMatchmaking.OnLobbyMemberJoined -= OnMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnMemberLeave;
    }
}