using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadiusHolder : MonoBehaviour
{
    public ChatDrawer chatManager;
    public GameUIManager uiManager;
    public float radiusThickness;

    public void SendRadius(bool isOn)
    {
        if (isOn)
        {
            uiManager.SetPenThickness(radiusThickness);
            chatManager.SetRadius(radiusThickness);
        }
    }
}
