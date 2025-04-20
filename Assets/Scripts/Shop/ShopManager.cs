using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [SerializeField]
    private List<PurchaseableObject> availableObjects;

    public Button leftDoodleButton;
    public Button rightDoodleButton;
    public Button leftExtraButton;
    public Button rightExtraButton;
    public Button leftHatButton;
    public Button rightHatButton;

    public Button purchaseButton;
    public Button doneButton;

    public Image doodleLines;
    public Image doodleColors;
    public Image extraImage;
    public Image hatImage;

    public TextMeshProUGUI priceText;
    public TextMeshProUGUI moneyText;

    private PlayerDataManager playerData;


    // Current indices for each object type
    private int currentDoodleIndex = 0;
    private int currentHatIndex = 0;
    private int currentExtraIndex = 0;

    // Filtered lists per type
    private List<PurchaseableObject> doodleObjects = new List<PurchaseableObject>();
    private List<PurchaseableObject> hatObjects = new List<PurchaseableObject>();
    private List<PurchaseableObject> extraObjects = new List<PurchaseableObject>();

    void Start()
    {
        playerData = PlayerDataManager.Instance;

        // Filter available objects by type
        FilterObjectsByType();

        // Set up button listeners
        leftDoodleButton.onClick.AddListener(() => NavigateObjects(PurchaseableObjectType.Doodle, -1));
        rightDoodleButton.onClick.AddListener(() => NavigateObjects(PurchaseableObjectType.Doodle, 1));

        leftHatButton.onClick.AddListener(() => NavigateObjects(PurchaseableObjectType.Hat, -1));
        rightHatButton.onClick.AddListener(() => NavigateObjects(PurchaseableObjectType.Hat, 1));

        leftExtraButton.onClick.AddListener(() => NavigateObjects(PurchaseableObjectType.Extra, -1));
        rightExtraButton.onClick.AddListener(() => NavigateObjects(PurchaseableObjectType.Extra, 1));

        purchaseButton.onClick.AddListener(PurchaseSelectedItems);

        // Initial display
        UpdateUIDisplay();
        UpdateMoneyText();
    }

    private void FilterObjectsByType()
    {
        doodleObjects.Clear();
        hatObjects.Clear();
        extraObjects.Clear();

        foreach (PurchaseableObject obj in availableObjects)
        {
            switch (obj.objectType)
            {
                case PurchaseableObjectType.Doodle:
                    doodleObjects.Add(obj);
                    break;
                case PurchaseableObjectType.Hat:
                    hatObjects.Add(obj);
                    break;
                case PurchaseableObjectType.Extra:
                    extraObjects.Add(obj);
                    break;
            }
        }
    }

    private void NavigateObjects(PurchaseableObjectType type, int direction)
    {
        switch (type)
        {
            case PurchaseableObjectType.Doodle:
                if (doodleObjects.Count > 0)
                {
                    currentDoodleIndex = (currentDoodleIndex + direction + doodleObjects.Count) % doodleObjects.Count;
                }
                break;

            case PurchaseableObjectType.Hat:
                if (hatObjects.Count > 0)
                {
                    currentHatIndex = (currentHatIndex + direction + hatObjects.Count) % hatObjects.Count;
                }
                break;

            case PurchaseableObjectType.Extra:
                if (extraObjects.Count > 0)
                {
                    currentExtraIndex = (currentExtraIndex + direction + extraObjects.Count) % extraObjects.Count;
                }
                break;
        }

        UpdateUIDisplay();
    }

    private void UpdateUIDisplay()
    {
        // Update doodle display
        if (doodleObjects.Count > 0)
        {
            PurchaseableObject doodle = doodleObjects[currentDoodleIndex];
            doodleLines.sprite = doodle.objectSprite;
            doodleColors.sprite = doodle.objectColors;
        }

        // Update hat display
        if (hatObjects.Count > 0)
        {
            PurchaseableObject hat = hatObjects[currentHatIndex];
            if (hat.objectSprite != null)
            {
                hatImage.sprite = hat.objectSprite;
                hatImage.color = new Color(1, 1, 1, 1);
            }
            else
            {
                hatImage.color = new Color(1, 1, 1, 0);
            }
        }

        // Update extra display
        if (extraObjects.Count > 0)
        {
            PurchaseableObject extra = extraObjects[currentExtraIndex];
            if (extra.objectSprite != null)
            {
                extraImage.sprite = extra.objectSprite;
                extraImage.color = new Color(1, 1, 1, 1);
            }
            else
            {
                extraImage.color = new Color(1, 1, 1, 0);
            }
        }

        // Check which items are unlocked and update UI accordingly
        CheckUnlockedStatus();
    }

    private void CheckUnlockedStatus()
    {
        int totalPrice = 0;
        bool allUnlocked = true;

        // Check doodle unlock status
        if (doodleObjects.Count > 0)
        {
            PurchaseableObject doodle = doodleObjects[currentDoodleIndex];
            if (!playerData.IsObjectUnlocked(doodle.objectID, doodle.objectType))
            {
                Debug.Log("Doodle is not unlocked");
                totalPrice += doodle.objectPrice;
                allUnlocked = false;
            }
        }

        // Check hat unlock status
        if (hatObjects.Count > 0)
        {
            PurchaseableObject hat = hatObjects[currentHatIndex];
            if (!playerData.IsObjectUnlocked(hat.objectID, hat.objectType))
            {
                Debug.Log("Hat is not unlocked");
                totalPrice += hat.objectPrice;
                allUnlocked = false;
            }
        }

        // Check extra unlock status
        if (extraObjects.Count > 0)
        {
            PurchaseableObject extra = extraObjects[currentExtraIndex];
            if (!playerData.IsObjectUnlocked(extra.objectID, extra.objectType))
            {
                Debug.Log("Extra is not unlocked");
                totalPrice += extra.objectPrice;
                allUnlocked = false;
            }
        }

        // Update UI based on unlock status
        if (allUnlocked)
        {
            purchaseButton.gameObject.SetActive(false);
            doneButton.gameObject.SetActive(true);
            priceText.text = "";
        }
        else
        {
            purchaseButton.gameObject.SetActive(true);
            doneButton.gameObject.SetActive(false);
            priceText.text = totalPrice.ToString();
        }
    }

    private void PurchaseSelectedItems()
    {
        int totalPrice = 0;
        List<PurchaseableObject> itemsToPurchase = new List<PurchaseableObject>();

        // Check which items need to be purchased
        if (doodleObjects.Count > 0)
        {
            PurchaseableObject doodle = doodleObjects[currentDoodleIndex];
            if (!playerData.IsObjectUnlocked(doodle.objectID, doodle.objectType))
            {
                totalPrice += doodle.objectPrice;
                itemsToPurchase.Add(doodle);
            }
        }

        if (hatObjects.Count > 0)
        {
            PurchaseableObject hat = hatObjects[currentHatIndex];
            if (!playerData.IsObjectUnlocked(hat.objectID, hat.objectType))
            {
                totalPrice += hat.objectPrice;
                itemsToPurchase.Add(hat);
            }
        }

        if (extraObjects.Count > 0)
        {
            PurchaseableObject extra = extraObjects[currentExtraIndex];
            if (!playerData.IsObjectUnlocked(extra.objectID, extra.objectType))
            {
                totalPrice += extra.objectPrice;
                itemsToPurchase.Add(extra);
            }
        }

        // Attempt purchase if player has enough coins
        if (playerData.SpendCoins(totalPrice))
        {
            foreach (PurchaseableObject item in itemsToPurchase)
            {
                playerData.UnlockObject(item);
            }

            // Update UI
            UpdateUIDisplay();
            UpdateMoneyText();
        }
        else
        {
            Debug.Log("Not enough coins to purchase selected items!");
            // Could display a message to the player here
        }
    }

    private void UpdateMoneyText()
    {
        moneyText.text = playerData.Coins.ToString();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            UpdateMoneyText();
        }
    }
}
