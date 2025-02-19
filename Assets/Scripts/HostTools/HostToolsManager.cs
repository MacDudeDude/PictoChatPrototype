using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Managing;
using System.Linq;
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
}
