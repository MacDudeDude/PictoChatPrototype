using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class ShopCarousel : MonoBehaviour
{
    public List<ShopItem> shopItems;
    public Button leftButton;
    public Button rightButton;

    public Button currentItemButton;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private int currentItemIndex = 0;
    private List<ShopItemSave> itemSaves = new List<ShopItemSave>();


    void Start()
    {
        Debug.Log("ShopCarousel: Start method called");
        leftButton.onClick.AddListener(onLeftButtonClick);
        rightButton.onClick.AddListener(onRightButtonClick);

        string filePath = Application.persistentDataPath + "/shopItems.json";
        Debug.Log("ShopCarousel: File path set to " + filePath);

        if (System.IO.File.Exists(filePath))
        {
            Debug.Log("ShopCarousel: Shop data file found");
            string jsonData = System.IO.File.ReadAllText(filePath);
            ShopItemSaveList savedItems = JsonUtility.FromJson<ShopItemSaveList>(jsonData);

            if (savedItems != null && savedItems.items != null)
            {
                Debug.Log("ShopCarousel: Loaded " + savedItems.items.Count + " saved shop items");
                itemSaves = savedItems.items;
            }
            else
            {
                Debug.Log("ShopCarousel: Saved data was null or empty, creating initial data");
                CreateInitialShopData(ref itemSaves);
            }
        }
        else
        {
            Debug.Log("ShopCarousel: No shop data file found, creating initial data");
            CreateInitialShopData(ref itemSaves);

            string jsonData = JsonUtility.ToJson(new ShopItemSaveList { items = itemSaves });
            System.IO.File.WriteAllText(filePath, jsonData);
            Debug.Log("ShopCarousel: Initial shop data saved to file");
        }

        UpdateCurrentItem();
        Debug.Log("ShopCarousel: Initial item updated, current index: " + currentItemIndex);
    }

    void CreateInitialShopData(ref List<ShopItemSave> saves)
    {
        Debug.Log("ShopCarousel: Creating initial shop data for " + shopItems.Count + " items");
        for (int i = 0; i < shopItems.Count; i++)
        {
            if (shopItems[i].itemID == 0)
            {
                saves.Add(new ShopItemSave(shopItems[i], true));
                Debug.Log("ShopCarousel: Added item " + shopItems[i].itemName + " (ID: " + shopItems[i].itemID + ") as purchased");
            }
            else
            {
                saves.Add(new ShopItemSave(shopItems[i]));
                Debug.Log("ShopCarousel: Added item " + shopItems[i].itemName + " (ID: " + shopItems[i].itemID + ") as not purchased");
            }
        }
        Debug.Log("ShopCarousel: Initial shop data creation complete");
    }


    void onLeftButtonClick()
    {
        currentItemIndex--;
        if (currentItemIndex < 0)
        {
            currentItemIndex = shopItems.Count - 1;
        }
        UpdateCurrentItem();
    }
    void onRightButtonClick()
    {
        currentItemIndex++;
        if (currentItemIndex >= shopItems.Count)
        {
            currentItemIndex = 0;
        }
        UpdateCurrentItem();
    }
    void UpdateCurrentItem()
    {
        Debug.Log("ShopCarousel: ItemSaves[i].itemID: " + itemSaves[currentItemIndex].itemID + " shopItems[currentItemIndex].itemID: " + shopItems[currentItemIndex].itemID + " itemSaves[i].isPurchased: " + itemSaves[currentItemIndex].isPurchased);

        ShopItemSave currentSave = itemSaves.Find(save => save.itemID == shopItems[currentItemIndex].itemID);

        Image buttonImage = currentItemButton.GetComponent<Image>();
        buttonImage.sprite = shopItems[currentItemIndex].itemIcon;

        float itemAlpha = currentSave.isPurchased ? 1.0f : 0.5f;
        Color buttonColor = buttonImage.color;
        buttonColor.a = itemAlpha;
        buttonImage.color = buttonColor;

        if (currentSave.isPurchased)
        {
            priceText.text = "Purchased";
            buyButton.gameObject.SetActive(false);
        }
        else
        {
            priceText.text = shopItems[currentItemIndex].itemPrice.ToString();
            buyButton.gameObject.SetActive(true);
        }
    }

}