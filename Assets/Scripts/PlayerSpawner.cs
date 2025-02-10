using FishNet;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
    }

    private void ServerManager_OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            GameObject playerInstance = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);
            Spawn(playerInstance, conn);
        }
    }
}
