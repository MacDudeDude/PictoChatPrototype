using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseManager : MonoBehaviour
{
    public GameObject hoveredObject;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        CheckMouseCollisions();
    }

    public GameObject GetHoveredObject()
    {
        return hoveredObject;
    }

    private void CheckMouseCollisions()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hitInfo = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);
        if(hitInfo.collider != null) {
            hoveredObject = hitInfo.collider.transform.root.gameObject;
        }else {
            hoveredObject = null;
        }
    }
}
