using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatDrawer : MonoBehaviour
{
    public enum ChatTools
    {
        drawing,
        erasing,
    }

    public ChatTools currentTool;
    public Color currentColor;
    public float currentRadius;
    public Bounds chatDrawBounds;

    public Grid drawGrid;
    public UnityEngine.Tilemaps.Tile tile;
    public int width;
    public int height;
    private int ppu;
    public TextureManager chatDrawingArea;
    private Color32[] textureColors;
    private Camera cam;
    private bool tilemapUpdated;
    private Vector3 lastMousePosition;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        chatDrawingArea.InitializeTextures(width, height, 1, tile.sprite.pixelsPerUnit);
        ppu = Mathf.RoundToInt(tile.sprite.pixelsPerUnit);

        Vector3 boundsCenter = transform.position;
        Vector3 boundsSize = Vector3.one;

        float ppuThingy = 1 / tile.sprite.pixelsPerUnit;
        boundsCenter.x += (width / 2 * ppuThingy);
        boundsCenter.y += (height / 2 * ppuThingy);
        boundsCenter.z = 0;

        boundsSize.x = width * ppuThingy;
        boundsSize.y = height * ppuThingy;

        chatDrawBounds = new Bounds(boundsCenter, boundsSize);
        textureColors = new Color32[width * height];

        ResetChatScreen();
    }

    // Update is called once per frame
    void Update()
    {
        //Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        //mousePos.z = 0;
        //if (chatDrawBounds.Contains(mousePos))
        //{
        //    if (currentTool == ChatTools.drawing)
        //    {
        //        DrawTool();
        //    }
        //    else if (currentTool == ChatTools.erasing)
        //    {
        //        EraseTool();
        //    }
        //}

        if (currentTool == ChatTools.drawing)
        {
            DrawTool();
        }
        else if (currentTool == ChatTools.erasing)
        {
            EraseTool();
        }
    }

    private void FixedUpdate()
    {
        if (tilemapUpdated)
        {
            UpdateTiles();
        }
    }

    void DrawTool()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridStartpoint = drawGrid.WorldToCell(lastMousePosition);
            Vector3Int gridEndpoint = drawGrid.WorldToCell(mousePos);
            DrawLine(gridStartpoint, gridEndpoint, currentRadius, currentColor);

            lastMousePosition = mousePos;
        }
    }

    void EraseTool()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridStartpoint = drawGrid.WorldToCell(lastMousePosition);
            Vector3Int gridEndpoint = drawGrid.WorldToCell(mousePos);
            DrawLine(gridStartpoint, gridEndpoint, currentRadius, Color.white);

            lastMousePosition = mousePos;
        }
    }

    void ResetChatScreen()
    {
        for (int i = 0; i < textureColors.Length; i++)
        {
            textureColors[i] = Color.white;
        }

        chatDrawingArea.SetPixels(textureColors, 0);
    }

    public void UpdateTiles()
    {
        chatDrawingArea.SetPixels(textureColors, 0);
        tilemapUpdated = false;
    }

    public void DrawLine(Vector3Int gridStartPoint, Vector3Int gridEndPoint, float radius, Color32 color)
    {
        float currentPlaceRadius = radius * (1f / ppu);

        int gridRadius = Mathf.CeilToInt(currentPlaceRadius / (1f / ppu));
        float squaredRadius = Mathf.Pow(currentPlaceRadius, 2);

        List<Vector2Int> line = GenerateLine(gridStartPoint.x, gridStartPoint.y, gridEndPoint.x, gridEndPoint.y);
        for (int i = 0; i < line.Count; i++)
        {
            Vector3Int centerGridPos = (Vector3Int)line[i];
            Vector2 centerGridWorldPos = drawGrid.CellToWorld(centerGridPos);
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector3Int currentGridPos = new Vector3Int(centerGridPos.x + x, centerGridPos.y + y);
                    Vector2 worldPos = drawGrid.CellToWorld(currentGridPos);

                    if ((worldPos - centerGridWorldPos).sqrMagnitude <= squaredRadius)
                    {
                        if (currentGridPos.x < 0 || currentGridPos.y < 0 || currentGridPos.x >= width || currentGridPos.y >= height)
                            continue;

                        QueTile(currentGridPos, color);
                    }
                }
            }
        }
    }

    private void QueTile(Vector3Int pos, Color32 color)
    {
        int i = (pos.y * width) + pos.x;
        textureColors[i] = color;

        tilemapUpdated = true;
    }

    public List<Vector2Int> GenerateLine(int x, int y, int x2, int y2)
    {
        List<Vector2Int> linePositions = new List<Vector2Int>();

        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);
        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            linePositions.Add(new Vector2Int(x, y));
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }

        return linePositions;
    }
}
