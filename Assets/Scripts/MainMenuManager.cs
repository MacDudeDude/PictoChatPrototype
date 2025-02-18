using System.Collections.Generic;
using UnityEngine;
using Steamworks.Data;
using TMPro;
using UnityEngine.UI;


public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyMenuButton;
    [SerializeField] private Button searchLobbyButton;
    [SerializeField] private Button joinLobbyButton;

    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject lobbyListPanel;
    [SerializeField] private GameObject joinLobbyPanel;
    [SerializeField] private TMP_InputField joinLobbyField;
    [SerializeField] private Transform lobbyListContent;
    [SerializeField] private GameObject lobbyEntryPrefab;
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private SteamLobbyManager _steamLobbyManager;

    private List<Lobby> _currentLobbies = new List<Lobby>();

    private void Start()
    {
        // Initialize buttons
        createLobbyButton.onClick.AddListener(CreateLobby);
        searchLobbyButton.onClick.AddListener(ShowLobbyList);
        joinLobbyMenuButton.onClick.AddListener(ShowJoinLobby);
        exitButton.onClick.AddListener(ExitGame);
        joinLobbyButton.onClick.AddListener(JoinLobby);

    }

    private async void CreateLobby()
    {
        await _steamLobbyManager.CreateLobbyAsync();
    }

    private async void ShowLobbyList()
    {
        createLobbyButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        lobbyListPanel.SetActive(true);


        // Get lobby list
        var lobbies = await _steamLobbyManager.SearchLobbiesAsync();
        if (lobbies == null) return;

        foreach (var lobby in lobbies)
        {
            Debug.Log($"Found lobby: {lobby.Id} ({lobby.MemberCount} players)");
            _currentLobbies.Add(lobby);

            // Instantiate lobby entry prefab
            GameObject lobbyEntryObj = Instantiate(lobbyEntryPrefab, lobbyListContent);

            // Get references to UI elements 
            Button lobbyButton = lobbyEntryObj.GetComponent<Button>();
            TextMeshProUGUI lobbyNameText = lobbyEntryObj.transform.Find("LobbyName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI lobbyMemmberText = lobbyEntryObj.transform.Find("MemberCount").GetComponent<TextMeshProUGUI>();

            // Set lobby info
            lobbyNameText.text = $"Lobby #{lobby.Id}";
            lobbyMemmberText.text = $"{lobby.MemberCount} / {lobby.MaxMembers}";

            // Add click handler
            lobbyButton.onClick.AddListener(async () => await _steamLobbyManager.JoinLobbyAsync(lobby.Id));
        }
    }

    private void ShowJoinLobby()
    {
        createLobbyButton.gameObject.SetActive(false);
        searchLobbyButton.gameObject.SetActive(false);
        joinLobbyMenuButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        joinLobbyPanel.SetActive(true);

        joinLobbyField.text = "";
    }

    private async void JoinLobby()
    {
        //await _steamLobbyManager.JoinLobbyAsync(ulong.Parse(joinLobbyField.text));
        await _steamLobbyManager.JoinLobbyWithCodeAsync(joinLobbyField.text);
    }


    private void ExitGame()
    {
        Application.Quit();
    }
}

