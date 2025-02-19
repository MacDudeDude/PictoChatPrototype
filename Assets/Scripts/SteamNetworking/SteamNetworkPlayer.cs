using FishNet.Object;
using Steamworks;
using UnityEngine;


public class SteamNetworkPlayer : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            // Get the local player's Steam ID and send it to the server
            ulong steamId = SteamClient.SteamId;
            SendSteamIdServerRpc(steamId);
        }
    }

    [ServerRpc]
    private void SendSteamIdServerRpc(ulong steamId)
    {
        // Register this connection with the Steam ID using the separate player manager.
        Debug.Log("Registering connection with Steam ID: " + steamId);
        SteamPlayerManager.Instance.RegisterConnection(Owner, steamId);
        DespawnPrespawnedPlayer();
    }

    public void DespawnPrespawnedPlayer()
    {
        base.Despawn();
    }
}