using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerDraw : MonoBehaviour
{
    public Grid grid;
    public Tilemap tilemap;
    private Vector3Int currentGridPos;
    private int[,] pixelgrid;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        currentGridPos = grid.WorldToCell(cam.ScreenToWorldPoint(Input.mousePosition));
        if(Input.GetMouseButton(0))
        {
            PlacePixel(currentGridPos);
        }else if(Input.GetMouseButton(1))
        {
            RemovePixel(currentGridPos);
        }
    }

    public void PlacePixel(Vector3Int pos)
    {
        if (pixelgrid[pos.x, pos.y] == 1)
            return;


    }

    public void RemovePixel(Vector3Int pos)
    {
        if (pixelgrid[pos.x, pos.y] == 0)
            return;


    }
}
