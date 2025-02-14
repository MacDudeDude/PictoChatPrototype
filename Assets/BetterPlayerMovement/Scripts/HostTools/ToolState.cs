using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolState : MonoBehaviour
{
    protected HostToolsManager hostTools;
    protected Camera cam;

    public virtual void Init(HostToolsManager tools)
    {
        hostTools = tools;
        cam = Camera.main;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
}
