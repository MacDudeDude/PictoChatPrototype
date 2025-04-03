using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDraggable
{
    bool CanDrag();
    void BeginDrag();
    void EndDrag(Vector3 dragEndVelocity);
    void UpdateDragPosition(Vector3 newPosition);
}
