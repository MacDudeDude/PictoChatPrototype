using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    private static PlayerDataManager _instance;
    public static PlayerDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("PlayerDataManager is null!");
            }
            return _instance;
        }
    }

    // Dev mode settings
    [Header("Development Settings")]
    public bool devModeEnabled = false;
    public int devModeCoinsToAdd = 100;

    private string saveFilePath;
    private PlayerData playerData;

    public int Coins { get { return playerData.coins; } }
    public List<UnlockedObject> UnlockedObjects { get { return playerData.unlockedObjects; } }

    // Selected item getters
    public int SelectedDoodleID { get { return playerData.selectedDoodleID; } }
    public int SelectedBackgroundID { get { return playerData.selectedBackgroundID; } }
    public int SelectedHatID { get { return playerData.selectedHatID; } }
    public int SelectedExtraID { get { return playerData.selectedExtraID; } }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, "playerData.json");
        LoadPlayerData();
    }

    void Update()
    {
        if (devModeEnabled)
        {
            HandleDevModeInput();
        }
    }

    private void HandleDevModeInput()
    {
        // Reset player data (Delete save file)
        if (Input.GetKeyDown(KeyCode.F10))
        {
            ResetPlayerData();
            Debug.Log("[DEV MODE] Player data reset");
        }

        // Add coins
        if (Input.GetKeyDown(KeyCode.F11))
        {
            AddCoins(devModeCoinsToAdd);
            Debug.Log($"[DEV MODE] Added {devModeCoinsToAdd} coins. New total: {playerData.coins}");
        }

        // Print player data to console
        if (Input.GetKeyDown(KeyCode.F12))
        {
            PrintPlayerDataDebug();
        }
    }

    private void PrintPlayerDataDebug()
    {
        Debug.Log($"[DEV MODE] === Player Data Debug ===");
        Debug.Log($"[DEV MODE] Coins: {playerData.coins}");
        Debug.Log($"[DEV MODE] Unlocked Objects: {playerData.unlockedObjects.Count}");
        Debug.Log($"[DEV MODE] Selected Doodle ID: {playerData.selectedDoodleID}");
        Debug.Log($"[DEV MODE] Selected Background ID: {playerData.selectedBackgroundID}");
        Debug.Log($"[DEV MODE] Selected Hat ID: {playerData.selectedHatID}");
        Debug.Log($"[DEV MODE] Selected Extra ID: {playerData.selectedExtraID}");
    }

    public void ResetPlayerData()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }

        playerData = new PlayerData();
        playerData.coins = 0;
        playerData.unlockedObjects = new List<UnlockedObject>();

        // Set default selected items
        playerData.selectedDoodleID = 0;
        playerData.selectedBackgroundID = 0;
        playerData.selectedHatID = 0;
        playerData.selectedExtraID = 0;

        // Unlock default items
        UnlockDefaultItems();

        SavePlayerData();
    }

    private void LoadPlayerData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Player data loaded. Coins: " + playerData.coins);
        }
        else
        {
            Debug.Log("No save file found. Creating new player data.");
            playerData = new PlayerData();
            playerData.coins = 0;
            playerData.unlockedObjects = new List<UnlockedObject>();

            // Set default selected items
            playerData.selectedDoodleID = 0;
            playerData.selectedBackgroundID = 0;
            playerData.selectedHatID = 0;
            playerData.selectedExtraID = 0;

            // Unlock default items
            UnlockDefaultItems();

            SavePlayerData();
        }
    }

    private void UnlockDefaultItems()
    {
        // Add default doodle (ID 0)
        UnlockedObject defaultDoodle = new UnlockedObject
        {
            objectID = 0,
            objectType = PurchaseableObjectType.Doodle
        };
        playerData.unlockedObjects.Add(defaultDoodle);

        // Add default background (ID 0)
        UnlockedObject defaultBackground = new UnlockedObject
        {
            objectID = 0,
            objectType = PurchaseableObjectType.Background
        };
        playerData.unlockedObjects.Add(defaultBackground);

        UnlockedObject noHat = new UnlockedObject
        {
            objectID = 0,
            objectType = PurchaseableObjectType.Hat
        };
        playerData.unlockedObjects.Add(noHat);

        UnlockedObject noExtra = new UnlockedObject
        {
            objectID = 0,
            objectType = PurchaseableObjectType.Extra
        };
        playerData.unlockedObjects.Add(noExtra);

        // Add default hat (ID 1)
        UnlockedObject defaultHat = new UnlockedObject
        {
            objectID = 1,
            objectType = PurchaseableObjectType.Hat
        };
        playerData.unlockedObjects.Add(defaultHat);

        // Add default extra (ID 1)
        UnlockedObject defaultExtra = new UnlockedObject
        {
            objectID = 1,
            objectType = PurchaseableObjectType.Extra
        };
        playerData.unlockedObjects.Add(defaultExtra);
    }

    public void SavePlayerData()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Player data saved");
    }

    public bool IsObjectUnlocked(int objectID, PurchaseableObjectType objectType)
    {
        return playerData.unlockedObjects.Exists(obj => obj.objectID == objectID && obj.objectType == objectType);
    }

    public void AddCoins(int amount)
    {
        playerData.coins += amount;
        SavePlayerData();
    }

    public bool SpendCoins(int amount)
    {
        if (playerData.coins >= amount)
        {
            playerData.coins -= amount;
            SavePlayerData();
            return true;
        }
        return false;
    }

    public void UnlockObject(PurchaseableObject obj)
    {
        if (!IsObjectUnlocked(obj.objectID, obj.objectType))
        {
            UnlockedObject newObj = new UnlockedObject
            {
                objectID = obj.objectID,
                objectType = obj.objectType
            };
            playerData.unlockedObjects.Add(newObj);
            SavePlayerData();
        }
    }

    // Methods to select items
    public void SelectDoodle(int doodleID)
    {
        playerData.selectedDoodleID = doodleID;
        SavePlayerData();
    }

    public void SelectBackground(int backgroundID)
    {
        playerData.selectedBackgroundID = backgroundID;
        SavePlayerData();
    }

    public void SelectHat(int hatID)
    {
        playerData.selectedHatID = hatID;
        SavePlayerData();
    }

    public void SelectExtra(int extraID)
    {
        playerData.selectedExtraID = extraID;
        SavePlayerData();
    }

    // Helper method to get unlocked items of specific type
    public List<int> GetUnlockedItemIDs(PurchaseableObjectType type)
    {
        List<int> results = new List<int>();

        foreach (UnlockedObject obj in playerData.unlockedObjects)
        {
            if (obj.objectType == type)
            {
                results.Add(obj.objectID);
            }
        }

        return results;
    }
}

[System.Serializable]
public class PlayerData
{
    public int coins;
    public List<UnlockedObject> unlockedObjects;

    // Currently selected items
    public int selectedDoodleID;
    public int selectedBackgroundID;
    public int selectedHatID;
    public int selectedExtraID;
}

[System.Serializable]
public class UnlockedObject
{
    public int objectID;
    public PurchaseableObjectType objectType;
    public string objectName;
}