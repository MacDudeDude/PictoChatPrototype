using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
public class PlayerCosmetics : NetworkBehaviour
{
    public SpriteRenderer doodleHolder;
    public SpriteRenderer doodleScribblesHolder;
    public SpriteRenderer hatHolder;
    public SpriteRenderer extraHolder;

    public List<PurchaseableObject> validObjects;

    private Sprite doodleSprite;
    private Sprite hatSprite;
    private Sprite extraSprite;
    private Color doodleColor;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            // Subscribe to cosmetics update event
            PlayerDataManager.OnCosmeticsUpdated += OnCosmeticsUpdated;

            // Apply cosmetics when the player joins
            ApplyCosmetics();
        }
    }

    // Handle cosmetics updates
    private void OnCosmeticsUpdated()
    {
        if (IsOwner)
        {
            ApplyCosmetics();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner)
        {
            // Unsubscribe from event when this object is destroyed
            PlayerDataManager.OnCosmeticsUpdated -= OnCosmeticsUpdated;
        }
    }

    public void ApplyCosmetics()
    {
        if (!IsOwner)
        {
            return;
        }
        foreach (PurchaseableObject obj in validObjects)
        {
            if (obj.objectType == PurchaseableObjectType.Doodle)
            {
                if (obj.objectID == PlayerDataManager.Instance.SelectedDoodleID)
                {
                    doodleSprite = obj.objectSprite;
                }
            }
            else if (obj.objectType == PurchaseableObjectType.Hat)
            {
                if (obj.objectID == PlayerDataManager.Instance.SelectedHatID)
                {
                    hatSprite = obj.objectSprite;
                }
            }
            else if (obj.objectType == PurchaseableObjectType.Extra)
            {
                if (obj.objectID == PlayerDataManager.Instance.SelectedExtraID)
                {
                    extraSprite = obj.objectSprite;
                }
            }
        }

        doodleColor = PlayerDataManager.Instance.GetDoodleColor();


        hatHolder.sprite = hatSprite;

        if (doodleSprite != null)
        {
            doodleHolder.sprite = doodleSprite;
            // Apply doodle color
            doodleScribblesHolder.color = doodleColor;
        }


        extraHolder.sprite = extraSprite;


        // Tell the server about our cosmetics changes
        SyncCosmeticsServerRpc();
    }

    [ServerRpc]
    private void SyncCosmeticsServerRpc()
    {
        // This RPC ensures that cosmetic changes get properly synchronized
        // across the network to all clients
        SyncCosmeticsClientRpc();
    }

    [ObserversRpc]
    private void SyncCosmeticsClientRpc()
    {
        // Don't apply for the owner as they've already set their cosmetics
        if (IsOwner)
            return;

        // Force update visuals for other players
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Apply the sprites if they've been set
        if (hatSprite != null)
        {
            hatHolder.sprite = hatSprite;
        }
        if (doodleSprite != null)
        {
            doodleHolder.sprite = doodleSprite;
            // Apply doodle color
            doodleScribblesHolder.color = doodleColor;
        }
        if (extraSprite != null)
        {
            extraHolder.sprite = extraSprite;
        }
    }
}
