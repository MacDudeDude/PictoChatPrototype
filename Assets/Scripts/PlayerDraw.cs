using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using FishNet.Object;
using FishNet;
using System; // Make PlayerDraw network aware.

/// <summary>
/// Handles drawing functionality for players, allowing them to paint on a grid-based canvas with multiple layers
/// </summary>
public class PlayerDraw : NetworkBehaviour
{
    [Header("References")]
    public TextureManager texManager;
    public Tile tile;
    public Grid grid;
    public Grid tilemapGrid;
    public GameObject tilemapPrefab;

    [Header("Layer Settings")]
    public int layersAmount;
    public int collisionLayer = 1;
    public Color32[] colorValues;

    [Header("Drawing Settings")]
    public float placeRadius;
    public int rows;
    public int collumns;
    public int width;
    public int height;

    [Header("State")]
    public int currentLayer;
    private int ppu; // Pixels per unit
    private Vector2 lastMousePosition;
    private Tilemap[,] tilemaps;
    private Tilemap playareaOutline;
    private Camera cam;

    // Grid data structures
    private List<int[,]> pixelgrid; // Stores the state of each pixel (0 or 1)
    private List<Color32[]> textureColors; // Stores the colors for each pixel

    // Tile update tracking
    private List<Vector3Int>[] updatedTilesPos;
    private List<TileBase>[] updatedTilesTile;
    private bool tilemapUpdated;

    /// <summary>
    /// Initializes the drawing canvas with specified dimensions and layers
    /// </summary>
    void Start()
    {


        cam = Camera.main;
        ppu = Mathf.RoundToInt(tile.sprite.pixelsPerUnit);


        // Initialize tile update tracking arrays
        updatedTilesPos = new List<Vector3Int>[rows * collumns];
        updatedTilesTile = new List<TileBase>[rows * collumns];
        for (int i = 0; i < rows * collumns; i++)
        {
            updatedTilesPos[i] = new List<Vector3Int>();
            updatedTilesTile[i] = new List<TileBase>();
        }

        // Create tilemap grid
        tilemaps = new Tilemap[rows, collumns];
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < collumns; y++)
            {
                Tilemap tempMap = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, grid.transform).GetComponent<Tilemap>();
                tilemaps[x, y] = tempMap;
            }
        }

        playareaOutline = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, grid.transform).GetComponent<Tilemap>();

        width *= rows;
        height *= collumns;

        // Initialize layer data
        currentLayer = collisionLayer;
        pixelgrid = new List<int[,]>();
        textureColors = new List<Color32[]>();
        for (int i = 0; i < layersAmount; i++)
        {
            pixelgrid.Add(new int[width, height]);
            textureColors.Add(new Color32[width * height]);
        }

        texManager.InitializeTextures(width, height, layersAmount, ppu);
        SetOutlineTiles();
    }

    /// <summary>
    /// Creates outline tiles around the drawing area boundaries
    /// </summary>
    public void SetOutlineTiles()
    {
        Vector3Int[] positions = new Vector3Int[width + 2];
        TileBase[] tiles = new TileBase[width + 2];

        //Bottom outline
        for (int x = 0; x < width + 2; x++)
        {
            positions[x] = new Vector3Int(x - 1, -1, 0);
            tiles[x] = tile;
        }
        playareaOutline.SetTiles(positions, tiles);

        //Top Outline
        for (int x = 0; x < width + 2; x++)
        {
            positions[x] = new Vector3Int(x - 1, height, 0);
            tiles[x] = tile;
        }
        playareaOutline.SetTiles(positions, tiles);

        positions = new Vector3Int[height + 2];
        tiles = new TileBase[width + 2];

        //Left outline
        for (int x = 0; x < height + 2; x++)
        {
            positions[x] = new Vector3Int(-1, x - 1, 0);
            tiles[x] = tile;
        }
        playareaOutline.SetTiles(positions, tiles);

        //Right Outline
        for (int x = 0; x < height + 2; x++)
        {
            positions[x] = new Vector3Int(width, x - 1, 0);
            tiles[x] = tile;
        }
        playareaOutline.SetTiles(positions, tiles);
    }

    /// <summary>
    /// Handles input and updates the drawing canvas each frame
    /// </summary>
    void Update()
    {
        // Process drawing input only on the owner.
        if (!IsOwner)
            return;

        HandleDrawingInput();
    }

    void HandleDrawingInput()
    {
        // If the mouse button was just pressed, record the starting position.
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // For adding drawing strokes.
        if (Input.GetMouseButton(0))
        {
            Vector2 currentMouse = cam.ScreenToWorldPoint(Input.mousePosition);
            ProcessAddDrawingServerRpc(lastMousePosition, currentMouse);
            lastMousePosition = currentMouse;
        }

        // For removing drawing strokes.
        if (Input.GetMouseButton(1))
        {
            Vector2 currentMouse = cam.ScreenToWorldPoint(Input.mousePosition);
            ProcessRemoveDrawingServerRpc(lastMousePosition, currentMouse);
            lastMousePosition = currentMouse;
        }

        // Clear all tiles when pressing R.
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearAllTilesServerRpc();
        }
    }

    #region RPC Methods

    //!TODO: we need one server rpc func that passes the lastPos and currentPos to an observer rpc that runs process segment
    /// <summary>
    /// Called on the server to process an "add" drawing segment.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    void ProcessAddDrawingServerRpc(Vector2 lastPos, Vector2 currentPos)
    {
        ProcessDrawingSegment(lastPos, currentPos, true);
        RpcUpdateTileData(textureColors[currentLayer], currentLayer);
    }

    /// <summary>
    /// Called on the server to process a "remove" drawing segment.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    void ProcessRemoveDrawingServerRpc(Vector2 lastPos, Vector2 currentPos)
    {
        ProcessDrawingSegment(lastPos, currentPos, false);
        RpcUpdateTileData(textureColors[currentLayer], currentLayer);
    }

    /// <summary>
    /// Called on the server when clearing the drawing.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    void ClearAllTilesServerRpc()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pixelgrid[currentLayer][x, y] != 0)
                {
                    Vector3Int currentGridPos = new Vector3Int(x, y, 0);
                    Vector3 worldPos = grid.CellToWorld(currentGridPos);
                    Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);

                    QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, null, 0);
                }
            }
        }
        RpcUpdateTileData(textureColors[currentLayer], currentLayer);
    }

    /// <summary>
    /// An Observers RPC that tells all clients to update their tilemaps and texture.
    /// </summary>
    [ObserversRpc]
    void RpcUpdateTileData(Color32[] updatedColors, int layer)
    {
        // Update the tilemaps based on the changes queued.
        for (int i = 0; i < updatedTilesPos.Length; i++)
        {
            if (updatedTilesPos[i].Count > 0)
            {
                (int x, int y) = To2DIndex(i);
                tilemaps[x, y].SetTiles(updatedTilesPos[i].ToArray(), updatedTilesTile[i].ToArray());
            }
            updatedTilesPos[i].Clear();
            updatedTilesTile[i].Clear();
        }
        // Update the texture with the new colors.
        texManager.SetPixels(updatedColors, layer);
        tilemapUpdated = false;
    }

    #endregion

    /// <summary>
    /// Helper method that processes a drawing segment.
    /// This replicates the inner loops of your original Adding()/Removing() logic.
    /// </summary>
    //!!TODO: change bool to int represting layer, and int for value
    void ProcessDrawingSegment(Vector2 lastPos, Vector2 currentPos, bool isAdding)
    {
        float currentPlaceRadius = placeRadius * (1f / ppu);
        int gridRadius = Mathf.CeilToInt(currentPlaceRadius / (1f / ppu));
        float squaredRadius = Mathf.Pow(currentPlaceRadius, 2);

        Vector3Int gridStartpoint = grid.WorldToCell(lastPos);
        Vector3Int gridEndpoint = grid.WorldToCell(currentPos);
        // Dummy conversion (if needed).
        Vector2 dummyWorld = grid.CellToWorld(gridEndpoint);

        List<Vector2Int> line = GenerateLine(gridStartpoint.x, gridStartpoint.y, gridEndpoint.x, gridEndpoint.y);
        foreach (Vector2Int point in line)
        {
            Vector3Int centerGridPos = new Vector3Int(point.x, point.y, 0);
            Vector2 centerGridWorldPos = grid.CellToWorld(centerGridPos);
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector3Int currentGridPos = new Vector3Int(centerGridPos.x + x, centerGridPos.y + y, 0);
                    Vector2 worldPos = grid.CellToWorld(currentGridPos);

                    if ((worldPos - centerGridWorldPos).sqrMagnitude <= squaredRadius)
                    {
                        if (currentGridPos.x < 0 || currentGridPos.y < 0 || currentGridPos.x >= width || currentGridPos.y >= height)
                            continue;

                        Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);

                        if (isAdding)
                        {
                            if (pixelgrid[currentLayer][currentGridPos.x, currentGridPos.y] == 1)
                                continue;

                            QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, tile, 1);
                        }
                        else
                        {
                            if (pixelgrid[currentLayer][currentGridPos.x, currentGridPos.y] == 0)
                                continue;

                            QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, null, 0);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Queues a tile update, modifying local pixelgrid and textureColors,
    /// and enqueuing a change for the tilemap if on the collision layer.
    /// </summary>
    void QueTile(int x, int y, Vector3Int pos, Tile tile, int value)
    {
        int i = To1DIndex(x, y);

        if (currentLayer == collisionLayer)
        {
            updatedTilesPos[i].Add(pos);
            updatedTilesTile[i].Add(tile);
        }

        pixelgrid[currentLayer][pos.x, pos.y] = value;

        i = (pos.y * width) + pos.x;
        textureColors[currentLayer][i] = colorValues[value];

        tilemapUpdated = true;
    }

    /// <summary>
    /// Converts 2D coordinates to a 1D array index
    /// </summary>
    public int To1DIndex(int x, int y)
    {
        return y * rows + x;
    }

    /// <summary>
    /// Converts a 1D array index to 2D coordinates
    /// </summary>
    public (int x, int y) To2DIndex(int i)
    {
        int x = i % rows;
        int y = i / rows;
        return (x, y);
    }

    /// <summary>
    /// Generates a line of points between two coordinates using Bresenham's line algorithm
    /// </summary>
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
