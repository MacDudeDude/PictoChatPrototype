using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet.Managing;

/// <summary>
/// Manages Steam lobby functionality including creation, joining, and searching for lobbies.
/// Integrates with FishNet networking to handle multiplayer connections.
/// </summary>
public class SteamLobbyManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the SteamLobbyManager.
    /// </summary>
    public static SteamLobbyManager Instance { get; private set; }

    [SerializeField]
    private int maxPlayers = 4;

    /// <summary>
    /// The currently joined Steam lobby, if any.
    /// </summary>
    public Lobby? CurrentLobby { get; private set; }

    /// <summary>
    /// List of Steam IDs for players currently connected to the lobby.
    /// </summary>
    public List<SteamId> ConnectedPlayers { get; private set; } = new List<SteamId>();

    private NetworkManager _networkManager;
    private FishyFacepunch.FishyFacepunch _transport;
    [SerializeField] private string gameSceneName = "Game";

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

        //Get NetworkManager reference
        _networkManager = FindFirstObjectByType<NetworkManager>();
        _transport = FindObjectOfType<FishyFacepunch.FishyFacepunch>();
        // Setup Steam callbacks
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

        DontDestroyOnLoad(gameObject);
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
        var createLobbyResult = await SteamMatchmaking.CreateLobbyAsync();
        if (!createLobbyResult.HasValue)
        {
            Debug.LogError("Failed to create lobby");
            return;
        }
        createLobbyResult.Value.SetGameServer(SteamClient.SteamId);

        // Start server
        _networkManager.ServerManager.StartConnection();

        var lobby = createLobbyResult.Value;
        lobby.SetData("artist", SteamClient.SteamId.ToString());
        Debug.Log("Lobby created: " + lobby.Id);
    }

    /// <summary>
    /// Joins a lobby using the specified Steam lobby ID.
    /// </summary>
    /// <param name="lobbyId">The Steam lobby ID to join.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task JoinLobbyAsync(ulong lobbyId)
    {
        Debug.Log("Attempting to join lobby with ID: " + lobbyId);

        var joinResult = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (joinResult.HasValue)
        {
            CurrentLobby = joinResult.Value;
            Debug.Log("Successfully joined lobby with ID: " + CurrentLobby.Value.Id);
        }
        else
        {
            Debug.LogError("Failed to join lobby with ID: " + lobbyId);
        }
    }

    /// <summary>
    /// Searches for available Steam lobbies.
    /// </summary>
    /// <returns>A Task that returns a list of found lobbies.</returns>
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
            Debug.Log("Transport address set to: " + steamId.ToString());
        }
        else
        {
            Debug.LogError("Failed to get game server data from lobby");
            return;
        }
        Debug.Log("Lobby entered: " + lobby.Id);
        _networkManager.ClientManager.StartConnection();
        Debug.Log("Client manager started");

        // Change scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Callback triggered when a game lobby join is requested (e.g., through Steam overlay).
    /// Attempts to join the requested lobby.
    /// </summary>
    /// <param name="lobby">The lobby to join.</param>
    /// <param name="id">The Steam ID of the friend whose lobby is being joined.</param>
    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        Debug.Log("Attempting to join lobby with ID: " + lobby.Id);

        var joinResult = await SteamMatchmaking.JoinLobbyAsync(lobby.Id);
        if (joinResult.HasValue)
        {
            CurrentLobby = joinResult.Value;
            Debug.Log("Successfully joined lobby with ID: " + CurrentLobby.Value.Id);
        }
        else
        {
            Debug.LogError("Failed to join lobby with ID: " + lobby.Id);
        }
    }

    public string getArtist()
    {
        var artist = CurrentLobby.Value.GetData("artist");
        return artist;
    }

    public List<Friend> getMembers()
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