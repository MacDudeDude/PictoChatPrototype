using FishNet;
using FishNet.Transporting;
using UnityEditor;
using UnityEngine;

public class ConnectionStarter : MonoBehaviour
{
    [SerializeField] private ConnectionType _connectionType;


    private void Awake()
    {
        switch (_connectionType)
        {
            case ConnectionType.Host:
                InstanceFinder.ServerManager.StartConnection();
                InstanceFinder.ClientManager.StartConnection();
                break;


            case ConnectionType.Client:
                InstanceFinder.ClientManager.StartConnection();
                break;

        }
    }

    public enum ConnectionType
    {
        Host,
        Client
    }
}