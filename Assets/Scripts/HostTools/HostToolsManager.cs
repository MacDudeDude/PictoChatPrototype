using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Managing;
using System.Linq;
using FishNet.Connection;

public class HostToolsManager : NetworkBehaviour
{
    public enum SelectedTool
    {
        Pen,
        Eraser,
        Hand
    }


    public SelectedTool selectedTool;
    public MouseManager mouse;
    public PlayerDraw drawer;

    public int startingState;
    public ToolState[] tools;
    public ToolStateMachine StateMachine { get; set; }

    public bool isSpawned;

    private static HostToolsManager _instance;
    public static HostToolsManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;

            StateMachine = new ToolStateMachine();
            for (int i = 0; i < tools.Length; i++)
            {
                tools[i].Init(this);
            }
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        foreach (var clientPair in NetworkManager.ServerManager.Clients)
        {
            if (clientPair.Value.IsLocalClient)
            {
                NetworkObject.GiveOwnership(clientPair.Value);
                break;
            }
        }

    }
    private void Start()
    {

        StateMachine.Initialize(tools[startingState]);
    }

    private void Update()
    {
        if (!IsOwner)
            return;
        StateMachine.CurrentToolState.FrameUpdate();
    }


    /// <summary>
    /// Changes the current artist by their ID
    /// </summary>
    public void ChangeArtist(string artistId)
    {
        ChangeArtistServerRpc(artistId);
    }

    [ServerRpc]
    private void ChangeArtistServerRpc(string artistId)
    {
        ChangeArtistObserversRpc(artistId);
        NetworkConnection client = SteamPlayerManager.Instance.GetNetworkConnection(ulong.Parse(artistId));
        NetworkObject.GiveOwnership(client);
    }
    /// <summary>
    /// Observers RPC to notify all clients of artist change
    /// </summary>
    [ObserversRpc(RunLocally = true)]
    private void ChangeArtistObserversRpc(string artistId)
    {
        SteamLobbyManager.Instance.ChangeArtist(artistId);
    }
}
