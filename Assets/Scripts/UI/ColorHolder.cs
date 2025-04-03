using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorHolder : MonoBehaviour
{
    public GameUIManager uiManager;
    public Color32 color;
    public Image image;

    public void SetColor(Color32 color)
    {
        this.color = color;
        image.color = color;
    }

    public void SendColor(bool isOn)
    {
        if (isOn)
            uiManager.SetPenColor(color);
    }

    public void OnValidate()
    {
        if (image != null)
        {
            image.color = color;
        }
    }
}
