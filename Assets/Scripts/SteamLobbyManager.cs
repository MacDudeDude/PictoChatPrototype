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
    public Lobby CurrentLobby { get; private set; }

    // Add this property to track connected players
    public List<SteamId> ConnectedPlayers { get; private set; } = new List<SteamId>();

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
\
        await CreateLobby();
    }

    private async Task CreateLobby()
    {
        var result = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (result.HasValue)
        {
            CurrentLobby = result.Value;
            Debug.Log("Lobby created with ID: " + CurrentLobby.Id);
            
            // Add the host to the connected players list
            ConnectedPlayers.Add(SteamClient.SteamId);
            
            // Set up lobby member callbacks
            SteamMatchmaking.OnLobbyMemberJoined += OnMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += OnMemberLeave;
        }
    }

    private void OnMemberJoined(Lobby lobby, Friend friend)
    {
        if (IsServer)
        {
            ConnectedPlayers.Add(friend.Id);
            UpdatePlayerListClientRpc(ConnectedPlayers.ToArray());
        }
    }

    private void OnMemberLeave(Lobby lobby, Friend friend)
    {
        if (IsServer)
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
} 