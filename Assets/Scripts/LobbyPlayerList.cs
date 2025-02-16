using FishNet.Object;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerList : MonoBehaviour
{
    // Assign your UI Text element via the Inspector.
    [SerializeField]
    private Text playersListText;  

    void Update()
    {
        if (SteamLobbyManager.Instance == null)
            return;

        UpdatePlayersListUI();
    }

    void UpdatePlayersListUI()
    {
        string listText = "Players in Lobby:\n";
        foreach (var steamId in SteamLobbyManager.Instance.ConnectedPlayers)
        {
            string playerName = new Friend(steamId).Name ?? "Unknown";
            listText += playerName + "\n";
        }

        playersListText.text = listText;
    }
}
