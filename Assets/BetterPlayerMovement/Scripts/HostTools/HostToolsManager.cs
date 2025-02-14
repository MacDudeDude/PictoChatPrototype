using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HostToolsManager : MonoBehaviour
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
    public PlayerDragManager dragger; 

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
        }
    }

    private void Update()
    {
        switch (selectedTool)
        {
            case SelectedTool.Pen:
                drawer.PenToolUpdate();
                break;
            case SelectedTool.Eraser:
                drawer.EraseToolUpdate();
                break;
            case SelectedTool.Hand:
                dragger.DragToolUpdate();
                break;
            default:
                break;
        }
    }
}
