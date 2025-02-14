using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseManager : MonoBehaviour
{
    public LayerMask hoverableLayers;
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
    
    public GameObject GetObjectBetweenTwoPoints(Vector2 pos1, Vector2 pos2)
    {
        RaycastHit2D hitInfo = Physics2D.Linecast(pos1, pos2, hoverableLayers);

        if (hitInfo.collider != null)
            return hitInfo.collider.gameObject;

        return null;
    }

    private void CheckMouseCollisions()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hitInfo = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, hoverableLayers);
        if(hitInfo.collider != null) {
            hoveredObject = hitInfo.collider.transform.root.gameObject;
        }else {
            hoveredObject = null;
        }
    }
}
