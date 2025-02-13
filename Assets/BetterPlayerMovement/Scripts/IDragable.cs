using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDragable
{
    void BeginDrag();

    void EndDrag(Vector3 dragEndVelocity);
}
