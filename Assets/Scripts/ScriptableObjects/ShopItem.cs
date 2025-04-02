using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Item", menuName = "Shop Item")]
public class ShopItem : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public int itemID;
    public int itemPrice;
    public Sprite itemIcon;
}

[System.Serializable]
public class ShopItemSave
{
    public ShopItemSave(ShopItem item, bool isPurchased = false)
    {
        itemID = item.itemID;
        this.isPurchased = isPurchased;
    }
    public int itemID;
    public bool isPurchased;

}

[System.Serializable]
public class ShopItemSaveList
{
    public List<ShopItemSave> items;
}
