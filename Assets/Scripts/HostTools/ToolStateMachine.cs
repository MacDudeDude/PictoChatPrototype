using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolStateMachine
{
    public ToolState CurrentToolState { get; set; }

    public void Initialize(ToolState startingState)
    {
        CurrentToolState = startingState;
        CurrentToolState.EnterState();
    }

    public void ChangeState(ToolState newState)
    {
        CurrentToolState.ExitState();
        CurrentToolState = newState;
        CurrentToolState.EnterState();
    }
}
