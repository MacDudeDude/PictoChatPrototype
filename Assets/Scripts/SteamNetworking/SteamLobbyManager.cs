using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using FishNet.Object;
using System;
using FishNet.Managing.Scened;
using UnityEditor.SearchService;

/// <summary>
/// Manages Steam lobby functionality including creation, joining, and searching for lobbies.
/// This object is not a network object so that lobby management remains independent of FishNet's network synchronization.
/// </summary>
public class SteamLobbyManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the SteamLobbyManager.
    /// </summary>
    public static SteamLobbyManager Instance { get; private set; }

    [SerializeField]
    private NetworkObject SteamPlayerManager;

    [SerializeField]
    private int maxPlayers = 4;

    /// <summary>
    /// The currently joined Steam lobby, if any.
    /// </summary>
    public Lobby? CurrentLobby { get; private set; }

    private FishyFacepunch.FishyFacepunch _transport;
    [SerializeField] private string gameSceneName = "Game";

    // General event for lobby metadata changes (if needed elsewhere)
    public event Action OnLobbyMetadataChanged;

    // Dedicated event for artist changes only
    public event Action<string> OnArtistChanged;

    SceneLoadData gameScene;

    /// <summary>
    /// Initializes the singleton instance and sets up Steam callbacks.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GameObject managerInstance = Instantiate(SteamPlayerManager.gameObject);
        InstanceFinder.ServerManager.Spawn(managerInstance);


        _transport = FindObjectOfType<FishyFacepunch.FishyFacepunch>();
        // Setup Steam callbacks
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    /// <summary>
    /// Processes Steam callbacks every frame.
    /// </summary>
    void Update()
    {
        SteamClient.RunCallbacks();
    }

    /// <summary>
    /// Creates a new Steam lobby and starts the server.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task CreateLobbyAsync()
    {
        var createLobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (!createLobbyResult.HasValue)
        {
            Debug.LogError("[SteamLobbyManager] Failed to create lobby");
            return;
        }
        createLobbyResult.Value.SetGameServer(SteamClient.SteamId);
        InstanceFinder.ServerManager.StartConnection();
        var lobby = createLobbyResult.Value;
        lobby.SetData("artist", SteamClient.SteamId.ToString());
        lobby.SetJoinable(true);
        lobby.SetPublic();

        Debug.Log("[SteamLobbyManager] Lobby created: " + lobby.Id);
    }

    /// <summary>
    /// Joins a lobby using the specified Steam lobby ID.
    /// </summary>
    /// <param name="lobbyId">The Steam lobby ID to join.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task JoinLobbyAsync(ulong lobbyId)
    {
        Debug.Log("[SteamLobbyManager] Attempting to join lobby with ID: " + lobbyId);
        var joinResult = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (joinResult.HasValue)
        {
            CurrentLobby = joinResult.Value;
            Debug.Log("[SteamLobbyManager] Successfully joined lobby with ID: " + CurrentLobby.Value.Id);
        }
        else
        {
            Debug.LogError("[SteamLobbyManager] Failed to join lobby with ID: " + lobbyId);
        }
    }

    /// <summary>
    /// Joins a lobby using the specified Steam lobby Code.
    /// </summary>
    /// <param name="lobbyId">The Steam lobby ID to join.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task JoinLobbyWithCodeAsync(string lobbyCode)
    {
        Debug.Log("[SteamLobbyManager] Attempting to join lobby with Code: " + lobbyCode);

        var lobbyQuery = SteamMatchmaking.LobbyList;
        lobbyQuery.WithMaxResults(1);
        lobbyQuery.WithSlotsAvailable(1);
        lobbyQuery.WithKeyValue("lobbyCode_deletethispartlater", lobbyCode);
        var lobbies = await lobbyQuery.RequestAsync();
        if (lobbies != null && lobbies.Length > 0)
        {
            var joinResult = await SteamMatchmaking.JoinLobbyAsync(lobbies[0].Id);
            if (joinResult.HasValue)
            {
                CurrentLobby = joinResult.Value;
                Debug.Log("[SteamLobbyManager] Successfully joined lobby with ID: " + CurrentLobby.Value.Id);
            }
            else
            {
                Debug.LogError("[SteamLobbyManager] Failed to join lobby with Code: " + lobbyCode);
            }
        }
        else
        {
            Debug.LogError("[SteamLobbyManager] Failed to find lobbey with Code: " + lobbyCode);
        }
    }

    /// <summary>
    /// Searches for available Steam lobbies.
    /// </summary>
    /// <returns>A Task that returns a list of found lobbies.</returns>
    public async Task<List<Lobby>> SearchLobbiesAsync()
    {
        Debug.Log("[SteamLobbyManager] Starting lobby search...");
        var lobbyQuery = SteamMatchmaking.LobbyList;
        lobbyQuery.WithMaxResults(5);
        lobbyQuery.WithSlotsAvailable(1);
        var lobbies = await lobbyQuery.RequestAsync();
        if (lobbies != null && lobbies.Length > 0)
        {
            Debug.Log("[SteamLobbyManager] Found " + lobbies.Length + " lobby/lobbies.");
            return new List<Lobby>(lobbies);
        }
        else
        {
            Debug.Log("[SteamLobbyManager] No lobbies found.");
            return new List<Lobby>();
        }
    }

    /// <summary>
    /// Callback triggered when successfully entering a lobby.
    /// Sets up the client connection and loads the game scene.
    /// </summary>
    /// <param name="lobby">The lobby that was entered.</param>
    private void OnLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        uint ip = 0;
        ushort port = 0;
        SteamId steamId = default;
        if (lobby.GetGameServer(ref ip, ref port, ref steamId))
        {
            _transport.SetClientAddress(steamId.ToString());
            Debug.Log("[SteamLobbyManager] Transport address set to: " + steamId.ToString());
        }
        else
        {
            Debug.LogError("[SteamLobbyManager] Failed to get game server data from lobby");
            return;
        }
        Debug.Log("[SteamLobbyManager] Lobby entered: " + lobby.Id);
        InstanceFinder.ClientManager.StartConnection();
        Debug.Log("[SteamLobbyManager] Client manager started");

        gameScene = new SceneLoadData(gameSceneName);
        gameScene.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadGlobalScenes(gameScene);

    }

    public void LeaveLobby()
    {
        CurrentLobby.Value.Leave();
        CurrentLobby = null;
    }
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            LeaveLobby();
        }
    }
    /// <summary>
    /// Callback triggered when a game lobby join is requested (e.g., through Steam overlay).
    /// Attempts to join the requested lobby.
    /// </summary>
    /// <param name="lobby">The lobby to join.</param>
    /// <param name="id">The Steam ID of the friend whose lobby is being joined.</param>
    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        Debug.Log("[SteamLobbyManager] Attempting to join lobby with ID: " + lobby.Id);
        var joinResult = await SteamMatchmaking.JoinLobbyAsync(lobby.Id);
        if (joinResult.HasValue)
        {
            CurrentLobby = joinResult.Value;
            Debug.Log("[SteamLobbyManager] Successfully joined lobby with ID: " + CurrentLobby.Value.Id);
        }
        else
        {
            Debug.LogError("[SteamLobbyManager] Failed to join lobby with ID: " + lobby.Id);
        }
    }

    /// <summary>
    /// Gets the currently selected artist ID from the lobby metadata.
    /// </summary>
    /// <returns>The artist ID stored in the lobby data, or null if not set.</returns>
    public string GetArtist()
    {
        var artist = CurrentLobby.Value.GetData("artist");
        return artist;
    }

    /// <summary>
    /// Updates the selected artist ID in the lobby metadata.
    /// </summary>
    /// <param name="artistId">The new artist ID to set for the lobby.</param>
    public void ChangeArtist(string artistId)
    {
        if (CurrentLobby == null)
            return;

        // Retrieve the previous artist value (if needed for comparison)
        string previousArtist = CurrentLobby.Value.GetData("artist");

        CurrentLobby.Value.SetData("artist", artistId);
        Debug.Log($"[SteamLobbyManager] Lobby metadata updated: artist = {artistId}");

        // Fire a general metadata change event.
        OnLobbyMetadataChanged?.Invoke();

        // Only fire the dedicated event if the artist has actually changed.
        if (artistId != previousArtist)
        {
            OnArtistChanged?.Invoke(artistId);
        }
    }

    /// <summary>
    /// Gets a list of all Steam friends currently in the lobby.
    /// </summary>
    /// <returns>A List of Friend objects representing the lobby members.</returns>
    public List<Friend> GetMembers()
    {
        List<Friend> members = new List<Friend>();
        foreach (var member in CurrentLobby.Value.Members)
        {
            members.Add(member);
        }
        return members;
    }


    /// <summary>
    /// Cleans up Steam callbacks when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Cleanup Steam callbacks
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

    }
}