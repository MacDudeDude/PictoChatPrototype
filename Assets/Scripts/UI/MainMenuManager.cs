using System.Collections.Generic;
using UnityEngine;
using Steamworks.Data;
using TMPro;
using UnityEngine.UI;


public class MainMenuManager : MonoBehaviour
{



    public async void CreateLobby()
    {
        await SteamLobbyManager.Instance.CreateLobbyAsync();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}

