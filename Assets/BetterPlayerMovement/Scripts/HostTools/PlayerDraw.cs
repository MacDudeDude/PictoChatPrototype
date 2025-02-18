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
}

/// <summary>
/// Handles networked drawing functionality for players in a multiplayer game.
/// Allows players to draw on a shared tilemap grid that syncs across the network.
/// </summary>
public class PlayerDraw : NetworkBehaviour
{
    /// <summary>Currently selected layer to draw on</summary>
    public int currentLayer;

    /// <summary>Radius of the drawing brush</summary>
    public float placeRadius;

    /// <summary>X bounds of the playable drawing area</summary>
    public Vector2 playAreaBoundsX;

    /// <summary>Y bounds of the playable drawing area</summary>
    public Vector2 playAreaBoundsY;

    /// <summary>Reference to the mouse input manager</summary>
    public MouseManager mouseManager;

    /// <summary>Reference to the texture manager</summary>
    public TextureManager texManager;

    /// <summary>Total number of drawing layers</summary>
    public int layersAmount;

    /// <summary>Layer used for collision detection</summary>
    public int collisionLayer = 1;

    /// <summary>Array of tile types that can be placed</summary>
    public Tile[] tileValues;

    /// <summary>Array of colors corresponding to tile values</summary>
    public Color32[] colorValues;

    /// <summary>Main Unity grid for world positioning</summary>
    public Grid grid;

    /// <summary>Grid for tilemap positioning</summary>
    public Grid tilemapGrid;

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

    /// <summary>List of all drawing commands for replay to new clients</summary>
    private List<DrawCommand> storedCommands = new List<DrawCommand>();

    /// <summary>
    /// Called when client starts. Sets up ownership and requests stored commands.
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();

        foreach (var clientPair in NetworkManager.ServerManager.Clients)
        {
            if (clientPair.Value.IsLocalClient)
            {
                NetworkObject.GiveOwnership(clientPair.Value);
                break;
            }
        }

        // After joining, ask the server to send the stored draw commands.
        // (This method will trigger a TargetRpc back to this client.)
        RequestStoredCommandsServerRpc();
    }

    /// <summary>
    /// Changes the current artist by their ID
    /// </summary>
    public void ChangeArtist(string artistId)
    {
        ChangeArtistServerRpc(artistId);
    }

    /// <summary>
    /// Server RPC to change the current artist
    /// </summary>
    [ServerRpc]
    private void ChangeArtistServerRpc(string artistId)
    {
        ChangeArtistObserversRpc(artistId);
        NetworkConnection client = SteamPlayerManager.Instance.GetNetworkConnection(ulong.Parse(artistId));
        NetworkObject.GiveOwnership(client);
    }

    /// <summary>
    /// Observers RPC to notify all clients of artist change
    /// </summary>
    [ObserversRpc(RunLocally = true)]
    private void ChangeArtistObserversRpc(string artistId)
    {
        SteamLobbyManager.Instance.ChangeArtist(artistId);
    }

    /// <summary>
    /// Initializes the drawing system, setting up tilemaps and textures
    /// </summary>
    void Start()
    {
        cam = Camera.main;
        ppu = Mathf.RoundToInt(tileValues[1].sprite.pixelsPerUnit);

        updatedTilesPos = new List<Vector3Int>[rows * collumns];
        updatedTilesTile = new List<TileBase>[rows * collumns];
        for (int i = 0; i < rows * collumns; i++)
        {
            updatedTilesPos[i] = new List<Vector3Int>();
            updatedTilesTile[i] = new List<TileBase>();
        }

        tilemaps = new Tilemap[rows, collumns];
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < collumns; y++)
            {
                Tilemap tempMap = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, grid.transform).GetComponent<Tilemap>();
                tilemaps[x, y] = tempMap;
            }
        }


        width *= rows;
        height *= collumns;

        currentLayer = collisionLayer;
        pixelgrid = new List<int[,]>();
        textureColors = new List<Color32[]>();
        for (int i = 0; i < layersAmount; i++)
        {
            pixelgrid.Add(new int[width, height]);
            textureColors.Add(new Color32[width * height]);
        }

        texManager.InitializeTextures(width, height, layersAmount, ppu);
        //GenerateBoundryColliders();
        SetOutlineTiles();

        playAreaBoundsX.y = width * (1f / ppu);
        playAreaBoundsY.y = height * (1f / ppu);
    }

    /* Generate box colliders are around the play area, only problem is that it's too good and better than the tilemaps which creates a noticeable difference
    public void GenerateBoundryColliders()
    {
        List<Vector2> boundryOffsets = new List<Vector2>();

        boundryOffsets.Add(new Vector2(1f, 0));
        boundryOffsets.Add(new Vector2(-1f, 0));
        boundryOffsets.Add(new Vector2(0, 1f));
        boundryOffsets.Add(new Vector2(0, -1f));

        for (int i = 0; i < boundryOffsets.Count; i++)
        {
            GameObject bottomCol = new GameObject("BottomCollider");
            bottomCol.transform.parent = transform;
            var col = bottomCol.AddComponent<BoxCollider2D>();

            float ppu = 1f / this.ppu;
            bottomCol.transform.position = new Vector3(width * ppu * 0.5f, height * ppu * 0.5f, 0) + new Vector3(boundryOffsets[i].x * width * ppu, boundryOffsets[i].y * height * ppu, 0);
            bottomCol.transform.localScale = new Vector3(width * ppu, height * ppu, 1);
        }
    }
    */

    /// <summary>
    /// Creates outline tiles around the play area boundary
    /// </summary>
    public void SetOutlineTiles()
    {
        playareaOutline = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, grid.transform).GetComponent<Tilemap>();

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
    /// Server RPC to draw a line between two points
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void DrawLineServerRpc(Vector3Int startPoint, Vector3Int endPoint, float radius, int value, int layer)
    {
        storedCommands.Add(new DrawCommand
        {
            startPoint = startPoint,
            endPoint = endPoint,
            radius = radius,
            value = value,
            layer = layer
        });

        DrawLineObserversRpc(startPoint, endPoint, radius, value, layer);
    }

    /// <summary>
    /// Observers RPC to sync line drawing across all clients
    /// </summary>
    [ObserversRpc]
    public void DrawLineObserversRpc(Vector3Int startPoint, Vector3Int endPoint, float radius, int value, int layer)
    {
        DrawLine(startPoint, endPoint, radius, value, layer);
    }

    /// <summary>
    /// Updates pen tool drawing based on mouse input
    /// </summary>
    public void PenToolUpdate()
    {
        if (!IsOwner)
            return;

        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

            if (mouseManager.GetObjectBetweenTwoPoints(mousePos, lastMousePosition) == null)
            {
                Vector3Int gridStartpoint = grid.WorldToCell(lastMousePosition);
                Vector3Int gridEndpoint = grid.WorldToCell(mousePos);
                DrawLineServerRpc(gridStartpoint, gridEndpoint, placeRadius, 1, currentLayer);
            }

            lastMousePosition = mousePos;
        }
    }

    /// <summary>
    /// Updates eraser tool based on mouse input
    /// </summary>
    public void EraseToolUpdate()
    {
        if (!IsOwner)
            return;

        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridStartpoint = grid.WorldToCell(lastMousePosition);
            Vector3Int gridEndpoint = grid.WorldToCell(mousePos);

            DrawLineServerRpc(gridStartpoint, gridEndpoint, placeRadius, 0, currentLayer);

            lastMousePosition = mousePos;
        }
    }

    /// <summary>
    /// Fixed update loop to handle tile updates
    /// </summary>
    private void FixedUpdate()
    {
        if (tilemapUpdated)
        {
            UpdateTiles();
        }
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
                    Vector3 worldPos = grid.CellToWorld(currentGridPos);
                    Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);

                    QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, null, 0, currentLayer);
                }
            }
        }

        tilemapUpdated = true;
    }

    /// <summary>
    /// Queues a tile update at the specified position
    /// </summary>
    private void QueTile(int x, int y, Vector3Int pos, Tile tile, int value, int layer)
    {
        int i = To1DIndex(x, y);

        if (layer == collisionLayer)
        {
            updatedTilesPos[i].Add(pos);
            updatedTilesTile[i].Add(tile);
        }

        pixelgrid[layer][pos.x, pos.y] = value;

        i = (pos.y * width) + pos.x;
        textureColors[layer][i] = colorValues[value];

        tilemapUpdated = true;
    }

    /// <summary>
    /// Draws a line between two points with given radius and value
    /// </summary>
    public void DrawLine(Vector3Int gridStartPoint, Vector3Int gridEndPoint, float radius, int value, int layer)
    {
        float currentPlaceRadius = radius * (1f / ppu);

        int gridRadius = Mathf.CeilToInt(currentPlaceRadius / (1f / ppu));
        float squaredRadius = Mathf.Pow(currentPlaceRadius, 2);

        List<Vector2Int> line = GenerateLine(gridStartPoint.x, gridStartPoint.y, gridEndPoint.x, gridEndPoint.y);
        for (int i = 0; i < line.Count; i++)
        {
            Vector3Int centerGridPos = (Vector3Int)line[i];
            Vector2 centerGridWorldPos = grid.CellToWorld(centerGridPos);
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector3Int currentGridPos = new Vector3Int(centerGridPos.x + x, centerGridPos.y + y);
                    Vector2 worldPos = grid.CellToWorld(currentGridPos);

                    if ((worldPos - centerGridWorldPos).sqrMagnitude <= squaredRadius)
                    {
                        if (currentGridPos.x < 0 || currentGridPos.y < 0 || currentGridPos.x >= width || currentGridPos.y >= height)
                            continue;

                        Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);

                        if (pixelgrid[layer][currentGridPos.x, currentGridPos.y] == value)
                            continue;

                        QueTile(currentTileMapPos.x, currentTileMapPos.y, currentGridPos, tileValues[value], value, layer);
                    }
                }
            }
        }
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

    /// <summary>
    /// Target RPC to send stored drawing commands to a specific client
    /// </summary>
    [TargetRpc]
    private void TargetSendStoredCommands(NetworkConnection target, DrawCommand[] commands)
    {
        // Replay each stored line command on the target client.
        foreach (var cmd in commands)
        {
            DrawLine(cmd.startPoint, cmd.endPoint, cmd.radius, cmd.value, cmd.layer);
        }
    }

    /// <summary>
    /// Server RPC for a client to request all stored drawing commands
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestStoredCommandsServerRpc(NetworkConnection sender = null)
    {
        TargetSendStoredCommands(sender, storedCommands.ToArray());
    }
}
