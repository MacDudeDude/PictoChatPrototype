using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HostToolsManager : MonoBehaviour
{
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
}
