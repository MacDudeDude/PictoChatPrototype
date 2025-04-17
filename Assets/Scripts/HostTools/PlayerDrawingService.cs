using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Tilemaps;
using FishNet.Managing;
using System.Linq;

/// <summary>
/// Represents a drawing command with position, size and layer information
/// </summary>
[System.Serializable]
public struct DrawCommand
{
    /// <summary>Starting grid position of the draw command</summary>
    public Vector3Int startPoint;

    /// <summary>Ending grid position of the draw command</summary> 
    public Vector3Int endPoint;

    /// <summary>Radius of the drawing brush</summary>
    public float radius;

    /// <summary>Value representing the tile/color to draw</summary>
    public int value;

    /// <summary>Layer to draw on</summary>
    public int layer;

    /// <summary>Color drawn</summary>
    public Color32 color;
}

/// <summary>
/// Now renamed to PlayerDrawingService to reflect its role as a service component.
/// Implements IDrawingService for decoupling and future extension.
/// </summary>
public class PlayerDrawingService : MonoBehaviour, IDrawingService
{
    [Header("Drawing Settings")]
    public int currentLayer;
    public float placeRadius;
    public Color32 currentColor;

    [Header("References")]
    public MouseManager mouseManager;
    public TextureManager texManager;
    public Grid collisionGrid;
    public Grid tilemapGrid;

    /// <summary>X bounds of the playable drawing area</summary>
    public Vector2 playAreaBoundsX;

    /// <summary>Y bounds of the playable drawing area</summary>
    public Vector2 playAreaBoundsY;

    /// <summary>Total number of drawing layers</summary>
    public int layersAmount;

    /// <summary>Layer used for collision detection</summary>
    public int collisionLayer = 1;

    /// <summary>Array of tile types that can be placed</summary>
    public Tile[] tileValues;

    /// <summary>Prefab for creating new tilemaps</summary>
    public GameObject tilemapPrefab;

    /// <summary>Number of rows in the tilemap grid</summary>
    public int rows;

    /// <summary>Number of columns in the tilemap grid</summary>
    public int collumns;

    /// <summary>Width of a single tilemap section</summary>
    public int width;

    /// <summary>Height of a single tilemap section</summary>
    public int height;

    // basically it just doesn't let the player draw above here for consistency reasons
    public int drawHeightOffset;

    private int ppu; // Pixels per unit
    private Vector2 lastMousePosition;
    private Tilemap[,] tilemaps;
    private Tilemap playareaOutline;
    private Camera cam;

    private List<int[,]> pixelgrid;
    private List<Color32[]> textureColors;

    private List<Vector3Int>[] updatedTilesPos;
    private List<TileBase>[] updatedTilesTile;
    private bool tilemapUpdated;

    public MouseManager MouseManager => mouseManager;
    public Grid CollisionGrid => collisionGrid;
    public float PlaceRadius => placeRadius;
    public int CurrentLayer => currentLayer;
    public Color32 CurrentColor => currentColor;

    /// <summary>
    /// Sets up the drawing service.
    /// Refactored to delegate initialization to multiple helper methods.
    /// </summary>
    void Start()
    {
        cam = Camera.main;
        ppu = Mathf.RoundToInt(tileValues[1].sprite.pixelsPerUnit);

        InitializeTilemapArrays();
        InitializeTilemaps();
        InitializePixelgridAndTextures();
        texManager.InitializeTextures(width, height, layersAmount, ppu);
        SetOutlineTiles();
        ConfigurePlayAreaBounds();
    }

    void FixedUpdate()
    {
        UpdateTiles();
    }


    /// <summary>
    /// Initializes arrays that track updated tile positions.
    /// </summary>
    private void InitializeTilemapArrays()
    {
        updatedTilesPos = new List<Vector3Int>[rows * collumns];
        updatedTilesTile = new List<TileBase>[rows * collumns];
        for (int i = 0; i < rows * collumns; i++)
        {
            updatedTilesPos[i] = new List<Vector3Int>();
            updatedTilesTile[i] = new List<TileBase>();
        }
    }

    /// <summary>
    /// Instantiates and organizes tilemap instances from the prefab.
    /// </summary>
    private void InitializeTilemaps()
    {
        tilemaps = new Tilemap[rows, collumns];
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < collumns; y++)
            {
                Tilemap tempMap = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, collisionGrid.transform)
                                    .GetComponent<Tilemap>();
                tilemaps[x, y] = tempMap;
            }
        }

        width *= rows;
        height *= collumns;
        height -= drawHeightOffset;
        currentLayer = collisionLayer;
    }

    /// <summary>
    /// Initializes the pixel grid and texture color arrays.
    /// </summary>
    private void InitializePixelgridAndTextures()
    {
        pixelgrid = new List<int[,]>();
        textureColors = new List<Color32[]>();
        for (int i = 0; i < layersAmount; i++)
        {
            pixelgrid.Add(new int[width, height]);
            textureColors.Add(new Color32[width * height]);
        }
    }

    /// <summary>
    /// Configures play area boundaries based on the tilemap dimensions.
    /// </summary>
    private void ConfigurePlayAreaBounds()
    {
        playAreaBoundsX.y = width * (1f / ppu);
        playAreaBoundsY.y = height * (1f / ppu);
    }

    /// <summary>
    /// Creates outline tiles around the play area boundary
    /// </summary>
    public void SetOutlineTiles()
    {
        playareaOutline = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, collisionGrid.transform).GetComponent<Tilemap>();

        Vector3Int[] positions = new Vector3Int[width + 2];
        TileBase[] tiles = new TileBase[width + 2];

        //Bottom outline
        for (int x = 0; x < width + 2; x++)
        {
            positions[x] = new Vector3Int(x - 1, -1, 0);
            tiles[x] = tileValues[1];
        }
        playareaOutline.SetTiles(positions, tiles);

        //Top Outline
        for (int x = 0; x < width + 2; x++)
        {
            positions[x] = new Vector3Int(x - 1, height, 0);
            tiles[x] = tileValues[1];
        }
        playareaOutline.SetTiles(positions, tiles);

        positions = new Vector3Int[height + 2];
        tiles = new TileBase[width + 2];

        //Left outline
        for (int x = 0; x < height + 2; x++)
        {
            positions[x] = new Vector3Int(-1, x - 1, 0);
            tiles[x] = tileValues[1];
        }
        playareaOutline.SetTiles(positions, tiles);

        //Right Outline
        for (int x = 0; x < height + 2; x++)
        {
            positions[x] = new Vector3Int(width, x - 1, 0);
            tiles[x] = tileValues[1];
        }
        playareaOutline.SetTiles(positions, tiles);
    }

    /// <summary>
    /// Updates all modified tiles and textures
    /// </summary>
    public void UpdateTiles()
    {
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

        texManager.SetPixels(textureColors[currentLayer], currentLayer);

        tilemapUpdated = false;
    }

    /// <summary>
    /// Clears all tiles from the current layer
    /// </summary>
    public void ClearAllTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pixelgrid[currentLayer][x, y] != 0)
                {
                    Vector3Int currentGridPos = new Vector3Int(x, y);
                    Vector3 worldPos = collisionGrid.CellToWorld(currentGridPos);
                    Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);

                    QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, null, 0, currentLayer, Color.clear);
                }
            }
        }

        tilemapUpdated = true;
    }

    /// <summary>
    /// Queues a tile update at the specified position
    /// </summary>
    private bool QueTile(int x, int y, Vector3Int pos, Tile tile, int value, int layer, Color32 color)
    {
        int i = To1DIndex(x, y);

        if (layer == collisionLayer)
        {
            if (pixelgrid[layer][pos.x, pos.y] != value) // We only need to update the collision tilemap if the value is different
            {
                updatedTilesPos[i].Add(pos);
                updatedTilesTile[i].Add(tile);
            }
        }

        if(pixelgrid[layer][pos.x, pos.y] == value)
            if (textureColors[layer][i].r == color.r && textureColors[layer][i].g == color.g && textureColors[layer][i].b == color.b)
                return false;

        pixelgrid[layer][pos.x, pos.y] = value;

        i = (pos.y * width) + pos.x;
        textureColors[layer][i] = color;

        tilemapUpdated = true;

        return true;
    }

    /// <summary>
    /// Draws a line between two points with given radius and value
    /// </summary>
    public bool DrawLine(Vector3Int gridStartPoint, Vector3Int gridEndPoint, float radius, int value, int layer, Color32 color)
    {
        float currentPlaceRadius = radius * (1f / ppu);
        int gridRadius = Mathf.CeilToInt(currentPlaceRadius / (1f / ppu));
        float squaredRadius = Mathf.Pow(currentPlaceRadius, 2);

        bool pixelChanged = false;
        List<Vector2Int> line = GenerateLine(gridStartPoint.x, gridStartPoint.y, gridEndPoint.x, gridEndPoint.y);
        foreach (Vector2Int point in line)
        {
            Vector3Int gridPoint = new Vector3Int(point.x, point.y, 0);
            if (ApplyBrushAtGridPosition(gridPoint, squaredRadius, gridRadius, value, layer, color))
                pixelChanged = true;
        }

        if(pixelChanged)
                    Debug.Log("[PlayerDrawingService] Drawing line");

        return pixelChanged;
    }

    /// <summary>
    /// Applies a brush stroke centered at the specified grid position.
    /// </summary>
    private bool ApplyBrushAtGridPosition(Vector3Int centerGridPos, float squaredRadius, int gridRadius, int value, int layer, Color32 color)
    {
        bool pixelChanged = false;
        Vector2 centerGridWorldPos = collisionGrid.CellToWorld(centerGridPos);
        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int y = -gridRadius; y <= gridRadius; y++)
            {
                Vector3Int currentGridPos = new Vector3Int(centerGridPos.x + x, centerGridPos.y + y, centerGridPos.z);
                Vector2 worldPos = collisionGrid.CellToWorld(currentGridPos);

                if ((worldPos - centerGridWorldPos).sqrMagnitude <= squaredRadius)
                {
                    if (currentGridPos.x < 0 || currentGridPos.y < 0 || currentGridPos.x >= width || currentGridPos.y >= height)
                        continue;

                    Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);
                    if (QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, tileValues[value], value, layer, color))
                        pixelChanged = true;
                }
            }
        }

        return pixelChanged;
    }

    /// <summary>
    /// Converts 2D coordinates to 1D index
    /// </summary>
    public int To1DIndex(int x, int y)
    {
        return y * rows + x;
    }

    /// <summary>
    /// Converts 1D index to 2D coordinates
    /// </summary>
    public (int x, int y) To2DIndex(int i)
    {
        int x = i % rows;
        int y = i / rows;
        return (x, y);
    }

    /// <summary>
    /// Generates points for a line between two coordinates using Bresenham's line algorithm
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

    private void OnDrawGizmos()
    {
        float ppu = Mathf.RoundToInt(40);
        Vector3 boundsCenter = transform.position;
        Vector3 boundsSize = Vector3.one;

        float width = this.width * rows;
        float height = this.height * collumns;

        float ppuThingy = 1 / ppu;
        boundsCenter.x += (width / 2 * ppuThingy);
        boundsCenter.y += (height / 2 * ppuThingy);
        boundsCenter.z = 0;

        boundsSize.x = width * ppuThingy;
        boundsSize.y = height * ppuThingy;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
        Gizmos.color = Color.red;

        Vector3 yOffset = Vector3.up * drawHeightOffset * ppuThingy;
        Gizmos.DrawLine(boundsCenter + Vector3.up * boundsSize.y / 2 - yOffset, boundsCenter + Vector3.up * boundsSize.y / 2 + Vector3.right * 100 - yOffset);
    }
}
