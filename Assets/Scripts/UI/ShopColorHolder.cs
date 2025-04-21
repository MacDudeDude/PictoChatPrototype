using UnityEngine;
using UnityEngine.UI;

public class ShopColorHolder : MonoBehaviour
{
    public Image colorImage;
    public Color color;


    public void SetColor(Color color)
    {
        this.color = color;
        colorImage.color = color;
    }

    public void OnValidate()
    {
        if (colorImage != null)
        {
            colorImage.color = color;
        }
    }

    public void SendColor()
    {
        PlayerDataManager.Instance.SetDoodleColor(color);
    }
}