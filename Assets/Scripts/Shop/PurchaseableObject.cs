using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum PurchaseableObjectType
{
    Background,
    Doodle,
    Hat,
    Extra
}

[CreateAssetMenu(fileName = "New Purchaseable Object", menuName = "Purchaseable Object")]
public class PurchaseableObject : ScriptableObject
{
    public PurchaseableObjectType objectType;
    public string objectName;
    public int objectPrice;
    public Sprite objectSprite;
    public Sprite objectColors;
    public int objectID;
}
