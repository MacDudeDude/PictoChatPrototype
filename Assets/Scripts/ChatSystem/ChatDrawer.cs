using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatDrawer : MonoBehaviour
{
    public float minTimeBetweenChats;
    public bool drawing;
    public bool erasing;
    public OverUI uiChecker;
    public ChatReciever recieverSender;
    public UnityEngine.UI.Toggle[] toggles;
    public UnityEngine.UI.Toggle[] colorToggles;
    public Color32[] colors;
    [Header("Self Drawing Variables")]
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
    private BoxCollider2D col;
    private float lastSentTimer;

    private bool startedInBounds = false;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        col = GetComponent<BoxCollider2D>();
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
        col.offset = chatDrawBounds.center - transform.position;
        col.size = chatDrawBounds.size;

        ResetChatScreen();

        recieverSender.Init(ppu, height, width);
    }

    public void SwitchToPenTool(bool enabled)
    {
        drawing = enabled;
    }

    public void SwitchToEraserTool(bool enabled)
    {
        erasing = enabled;
    }

    public void SetRadius()
    {
        if(toggles[0].isOn)
        {
            currentRadius = 0.1f;
        }else if (toggles[1].isOn)
        {
            currentRadius = 1.1f;
        }else if(toggles[2].isOn)
        {
            currentRadius = 2.1f;
        }
    }

    public void SetColor()
    {
        currentColor = Color.magenta; // For debugging

        for (int i = 0; i < colorToggles.Length; i++)
        {
            if(colorToggles[i].isOn)
            {
                currentColor = colors[i];
                break;
            }
        }
    }

    public void SendChatMessage()
    {
        if (lastSentTimer > 0)
            return;
        lastSentTimer = minTimeBetweenChats;

        recieverSender.SendChatMessage(textureColors);
        ResetChatScreen();
    }

    public void EnableDraw()
    {
        col.enabled = true;
    }

    public void DisableDraw()
    {
        col.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        lastSentTimer -= Time.deltaTime;

        if (drawing)
        {
            DrawTool();
        }
        else if (erasing)
        {
            EraseTool();
        }
    }

    private bool InBounds()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        return chatDrawBounds.Contains(mousePos);
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
        {
            startedInBounds = InBounds();
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0) && startedInBounds)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

            if (!uiChecker.IsPointerOverUIElement() && !uiChecker.IsPointOverElement(cam.WorldToScreenPoint(mousePos + ((Vector2)lastMousePosition - mousePos))))
            {
                Vector3Int gridStartpoint = drawGrid.WorldToCell(lastMousePosition);
                Vector3Int gridEndpoint = drawGrid.WorldToCell(mousePos);
                DrawLine(gridStartpoint, gridEndpoint, currentRadius, currentColor);
            }

            lastMousePosition = mousePos;
        }
    }

    void EraseTool()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startedInBounds = InBounds();
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0) && startedInBounds)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

            if (!uiChecker.IsPointerOverUIElement() && !uiChecker.IsPointOverElement(cam.WorldToScreenPoint(mousePos + ((Vector2)lastMousePosition - mousePos))))
            {
                Vector3Int gridStartpoint = drawGrid.WorldToCell(lastMousePosition);
                Vector3Int gridEndpoint = drawGrid.WorldToCell(mousePos);
                DrawLine(gridStartpoint, gridEndpoint, currentRadius, Color.clear);
            }

            lastMousePosition = mousePos;
        }
    }

    public void ResetChatScreen()
    {
        for (int i = 0; i < textureColors.Length; i++)
        {
            textureColors[i] = Color.clear;
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

    private void OnDrawGizmos()
    {
        ppu = Mathf.RoundToInt(tile.sprite.pixelsPerUnit);

        Vector3 boundsCenter = transform.position;
        Vector3 boundsSize = Vector3.one;

        float ppuThingy = 1 / tile.sprite.pixelsPerUnit;
        boundsCenter.x += (width / 2 * ppuThingy);
        boundsCenter.y += (height / 2 * ppuThingy);
        boundsCenter.z = 0;

        boundsSize.x = width * ppuThingy;
        boundsSize.y = height * ppuThingy;

        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}
