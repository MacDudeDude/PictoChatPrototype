
using UnityEngine;
using TMPro;
public class PlayerMenu : MonoBehaviour
{
    [SerializeField] private GameObject playerMenu;
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
        foreach (var member in members)
        {
            Debug.Log($"Member {member.Name}");
            // Assuming the text objects are named "Text1", "Text2", etc.
            int memberIndex = members.IndexOf(member);
            GameObject textObject = GameObject.Find($"Player{memberIndex + 1}");
            if (textObject != null)
            {
                if (member.Id.ToString() == artistId)
                {
                    textObject.GetComponent<TextMeshProUGUI>().text = member.Name + " (Artist)";
                }
                else
                {
                    textObject.GetComponent<TextMeshProUGUI>().text = member.Name;
                }
                textObject.SetActive(true);
            }
        }
    }
}
