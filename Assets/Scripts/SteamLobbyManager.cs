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

    private async Task CreateLobby()
    {

        // Create the lobby and await the result.
        var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (lobbyResult.HasValue)
        {
            CurrentLobby = lobbyResult.Value;
            Debug.Log("Lobby created with ID: " + CurrentLobby?.Id);
            // Add the host to the connected players list.
            ConnectedPlayers.Add(SteamClient.SteamId);

        }
        else
        {
            Debug.LogError("Failed to create lobby!");
        }
    }

    /// <summary>
    /// Joins a lobby using the specified Steam lobby ID.
    /// </summary>
    /// <param name="lobbyId">The Steam lobby ID (as ulong) to join.</param>
    public async Task JoinLobbyAsync(ulong lobbyId)
    {
        Debug.Log("Attempting to join lobby with ID: " + lobbyId);

        var joinResult = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (joinResult.HasValue)
        {
            CurrentLobby = joinResult.Value;
            Debug.Log("Successfully joined lobby with ID: " + CurrentLobby.Value.Id);

            // Add your SteamId to the ConnectedPlayers list if not already present.
            if (!ConnectedPlayers.Contains(SteamClient.SteamId))
                ConnectedPlayers.Add(SteamClient.SteamId);
        }
        else
        {
            Debug.LogError("Failed to join lobby with ID: " + lobbyId);
        }
    }

    /// <summary>
    /// Searches for available lobbies.
    /// </summary>
    /// <returns>A list of found lobbies.</returns>
    public async Task<List<Lobby>> SearchLobbiesAsync()
    {
        Debug.Log("Starting lobby search...");
        var lobbyQuery = SteamMatchmaking.LobbyList;

        lobbyQuery.WithMaxResults(5);
        lobbyQuery.WithSlotsAvailable(1);

        var lobbies = await lobbyQuery.RequestAsync();
        if (lobbies != null && lobbies.Length > 0)
        {
            Debug.Log("Found " + lobbies.Length + " lobby/lobbies.");
            return new List<Lobby>(lobbies);
        }
        else
        {
            Debug.Log("No lobbies found.");
            return new List<Lobby>();
        }
    }
}