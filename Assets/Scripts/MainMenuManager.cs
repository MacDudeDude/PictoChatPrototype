using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Transporting;
using FishyFacepunch;
using Steamworks;
using Steamworks.Data;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject lobbyListPanel;
    [SerializeField] private Transform lobbyListContent;
    [SerializeField] private GameObject lobbyEntryPrefab;
    [SerializeField] private string gameSceneName = "Game";

    private FishyFacepunch.FishyFacepunch _transport;


    private NetworkManager _networkManager;
    private List<Lobby> _currentLobbies = new List<Lobby>();

    private void Start()
    {
        // Initialize buttons
        createLobbyButton.onClick.AddListener(CreateLobby);
        joinLobbyButton.onClick.AddListener(ShowLobbyList);
        exitButton.onClick.AddListener(ExitGame);

        // Get NetworkManager reference
        _networkManager = FindFirstObjectByType<NetworkManager>();
        _transport = FindObjectOfType<FishyFacepunch.FishyFacepunch>();
        // Setup Steam callbacks
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private async void CreateLobby()
    {
        // Create Steam lobby
        var createLobbyResult = await SteamMatchmaking.CreateLobbyAsync();
        if (!createLobbyResult.HasValue)
        {
            Debug.LogError("Failed to create lobby");
            return;
        }

        var lobby = createLobbyResult.Value;
        Debug.Log("Lobby created: " + lobby.Id);
        // Start server
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();




    }

    private async void ShowLobbyList()
    {
        createLobbyButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        lobbyListPanel.SetActive(true);

        // Clear existing entries
        foreach (Transform child in lobbyListContent)
        {
            Destroy(child.gameObject);
        }
        _currentLobbies.Clear();

        // Get lobby list
        var lobbies = await SteamMatchmaking.LobbyList.RequestAsync();
        if (lobbies == null) return;

        foreach (var lobby in lobbies)
        {
            Debug.Log("Lobby: " + lobby.Id);
            _currentLobbies.Add(lobby);

            // Create UI entry
            var entry = Instantiate(lobbyEntryPrefab, lobbyListContent);
            var button = entry.GetComponent<Button>();
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();

            text.text = $"Lobby {lobby.Id}";
            button.onClick.AddListener(() => JoinLobby(lobby));
        }
    }

    private void JoinLobby(Lobby lobby)
    {
        lobby.Join();
        lobbyListPanel.SetActive(false);
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        // Start client only (server is already running on host)
        if (_networkManager.IsServerStarted)
        {
            _transport.SetClientAddress(lobby.Id.ToString());
            _networkManager.ClientManager.StartConnection();
        }

        // Change scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        lobby.Join();
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private void OnDestroy()
    {
        // Cleanup Steam callbacks
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }
}

