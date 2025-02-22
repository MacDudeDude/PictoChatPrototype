using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMinigame1 : MonoBehaviour
{
    void Start()
    {
    }

    void OnMouseDown()
    {
        Debug.Log($"[TestMinigame1] Clicked on TestMinigame1");
        MinigameManager.Instance.EndMinigame();
    }
}
