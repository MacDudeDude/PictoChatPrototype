using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using FishNet.Connection;

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


    private FishyFacepunch.FishyFacepunch _transport;
    [SerializeField] private string gameSceneName = "Game";

    /// <summary>
    /// Maps Steam IDs to NetworkConnections and vice versa
    /// </summary>
    private Dictionary<ulong, NetworkConnection> _steamToNetworkConnection = new Dictionary<ulong, NetworkConnection>();
    private Dictionary<NetworkConnection, ulong> _networkConnectionToSteam = new Dictionary<NetworkConnection, ulong>();

    /// <summary>
    /// Gets all currently connected Steam IDs
    /// </summary>
    public IEnumerable<ulong> ConnectedSteamIds => _steamToNetworkConnection.Keys;

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

        //Get transport reference
        _transport = FindObjectOfType<FishyFacepunch.FishyFacepunch>();
        // Setup Steam callbacks
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

        // Subscribe to FishNet connection events
        InstanceFinder.ServerManager.OnRemoteConnectionState += HandleConnectionState;

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
        InstanceFinder.ServerManager.StartConnection();

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
        InstanceFinder.ClientManager.StartConnection();
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

    public void ChangeArtist(string artistId)
    {
        CurrentLobby.Value.SetData("artist", artistId);
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
    /// Handles client connection/disconnection events
    /// </summary>
    private void HandleConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState != RemoteConnectionState.Started)
        {
            Debug.Log($"Client disconnected. Removing connection {connection.ClientId}");
            RemoveConnection(connection);
        }
    }

    /// <summary>
    /// Registers a new connection with its Steam ID
    /// </summary>
    public void RegisterConnection(NetworkConnection connection, ulong steamId)
    {
        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("Attempted to register connection but server is not started");
            return;
        }

        _steamToNetworkConnection[steamId] = connection;
        _networkConnectionToSteam[connection] = steamId;
        Debug.Log($"Registered new connection - Steam ID: {steamId}, Connection ID: {connection.ClientId}");
    }

    /// <summary>
    /// Removes a connection when client disconnects
    /// </summary>
    public void RemoveConnection(NetworkConnection connection)
    {
        if (_networkConnectionToSteam.TryGetValue(connection, out ulong steamId))
        {
            _steamToNetworkConnection.Remove(steamId);
            _networkConnectionToSteam.Remove(connection);
            Debug.Log($"Removed connection - Steam ID: {steamId}, Connection ID: {connection.ClientId}");
        }
        else
        {
            Debug.LogWarning($"Attempted to remove connection {connection.ClientId} but no Steam ID mapping found");
        }
    }

    /// <summary>
    /// Gets NetworkConnection by Steam ID
    /// </summary>
    public NetworkConnection GetNetworkConnection(ulong steamId)
    {
        _steamToNetworkConnection.TryGetValue(steamId, out NetworkConnection connection);
        if (connection == null)
        {
            Debug.LogWarning($"No NetworkConnection found for Steam ID: {steamId}");
        }
        else
        {
            Debug.Log($"Found NetworkConnection {connection.ClientId} for Steam ID: {steamId}");
        }
        return connection;
    }

    /// <summary>
    /// Gets Steam ID by NetworkConnection
    /// </summary>
    public ulong GetSteamId(NetworkConnection connection)
    {
        _networkConnectionToSteam.TryGetValue(connection, out ulong steamId);
        if (steamId == 0)
        {
            Debug.LogWarning($"No Steam ID found for connection: {connection.ClientId}");
        }
        else
        {
            Debug.Log($"Found Steam ID {steamId} for connection: {connection.ClientId}");
        }
        return steamId;
    }

    /// <summary>
    /// Cleans up Steam callbacks when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Cleanup Steam callbacks
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

        // Unsubscribe from FishNet connection events
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState -= HandleConnectionState;
        }
    }
}