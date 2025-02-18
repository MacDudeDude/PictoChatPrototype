using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Managing;
using UnityEngine.UI;

public class PlayerMenu : MonoBehaviour
{
    [SerializeField] private GameObject playerMenu;
    [SerializeField] private TextMeshProUGUI[] playerTexts;
    [SerializeField] private Button[] playerButtons;
    [SerializeField] private PlayerDraw playerDraw;

    void Start()
    {
        // Get buttons from the text components
        playerButtons = new Button[playerTexts.Length];
        for (int i = 0; i < playerTexts.Length; i++)
        {
            playerButtons[i] = playerTexts[i].GetComponent<Button>();
        }
        updatePlayerMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerMenu.SetActive(!playerMenu.activeSelf);
            updatePlayerMenu();
        }
    }

    public void updatePlayerMenu()
    {
        var members = SteamLobbyManager.Instance.getMembers();
        var artistId = SteamLobbyManager.Instance.getArtist();

        // Hide all text fields initially
        for (int i = 0; i < playerTexts.Length; i++)
        {
            playerTexts[i].gameObject.SetActive(false);
            if (playerButtons[i] != null)
            {
                playerButtons[i].onClick.RemoveAllListeners();
            }
        }

        // Update active players
        for (int i = 0; i < members.Count && i < playerTexts.Length; i++)
        {
            var member = members[i];
            Debug.Log($"Member {member.Name}");

            if (member.Id.ToString() == artistId)
            {
                playerTexts[i].text = member.Name + " (Artist)";
            }
            else
            {
                playerTexts[i].text = member.Name;
            }
            playerTexts[i].gameObject.SetActive(true);

            // Add click listener
            if (playerButtons[i] != null)
            {
                var memberId = member.Id.ToString();
                playerButtons[i].onClick.AddListener(() =>
                {
                    playerDraw.ChangeArtist(memberId);
                });
            }
        }
        foreach (var client in InstanceFinder.ServerManager.Clients)
        {
            Debug.Log($"Client ID:{client.Key} - {client.Value}");
        }
    }
}
