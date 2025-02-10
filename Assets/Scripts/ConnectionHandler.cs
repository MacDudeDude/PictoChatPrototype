using FishNet;

using UnityEngine;
using FishNet.Object;

public class ConnectionHandler : MonoBehaviour
{
    [SerializeField] private ConnectionType _connectionType;

    public GameObject drawingBoardPrefab;
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