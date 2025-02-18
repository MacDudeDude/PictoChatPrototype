using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDrawTool : ToolState
{
    public override void EnterState()
    {
        base.EnterState();
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        hostTools.drawer.PenToolUpdate();
    }
}
