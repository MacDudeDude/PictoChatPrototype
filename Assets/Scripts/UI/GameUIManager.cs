using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public PlayerDrawingService drawService;

    public void SetPenColor(Color32 color)
    {
        drawService.currentColor = color;
    }
}
