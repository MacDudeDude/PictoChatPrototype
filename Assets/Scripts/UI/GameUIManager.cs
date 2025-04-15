using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GameUIManager : MonoBehaviour
{
    public PlayerDrawingService drawService;
    public Image penFillMask;
    public TextMeshProUGUI artistName;

    public void Start()
    {
        penFillMask.color = drawService.currentColor;
        UpdateArtistName();
    }


    private void OnEnable()
    {
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnArtistChanged += OnArtistChanged;
        }
    }

    private void OnDisable()
    {
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnArtistChanged -= OnArtistChanged;
        }
    }

    private void OnArtistChanged(string artistId)
    {
        UpdateArtistName();
    }

    private void UpdateArtistName()
    {
        var artistId = SteamLobbyManager.Instance.GetArtist();
        var members = SteamLobbyManager.Instance.GetMembers();

        foreach (var member in members)
        {
            if (member.Id == artistId)
            {
                artistName.text = member.Name + " is hosting";
                break;
            }
        }
    }

    public void SetPenColor(Color32 color)
    {
        drawService.currentColor = color;
        penFillMask.color = color;
    }

    public void SetPenThickness(float thickness)
    {
        drawService.placeRadius = thickness;
    }
}
