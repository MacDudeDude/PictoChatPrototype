
using UnityEngine;
using TMPro;
public class PlayerMenu : MonoBehaviour
{
    [SerializeField] private GameObject playerMenu;
    [SerializeField] private TextMeshProUGUI[] playerTexts;

    void Start()
    {
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
        foreach (var text in playerTexts)
        {
            text.gameObject.SetActive(false);
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
        }
    }
}
