using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerDraw : MonoBehaviour
{
    public float placeRadius;
    public Tile tile;
    public Grid grid;
    public Grid tilemapGrid;
    public GameObject tilemapPrefab;

    public int rows;
    public int collumns;
    public int width;
    public int height;

    private int ppu;
    private Vector2 lastMousePosition;
    private Tilemap[,] tilemaps;
    private Tilemap playareaOutline;
    private int[,] pixelgrid;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        ppu = Mathf.RoundToInt(tile.sprite.pixelsPerUnit);
        
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
        pixelgrid = new int[width,height];

        SetOutlineTiles();
    }

    public void SetOutlineTiles()
    {
        Vector3Int[] positions = new Vector3Int[width + 2];
        TileBase[] tiles = new TileBase[width + 2];

        //Bottom outline
        for (int x = 0; x < width + 2; x++) {
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

    // Update is called once per frame
    void Update()
    {
        Adding();
        Removing();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            ClearAllTiles();
        }
        //currentGridPos = grid.WorldToCell(cam.ScreenToWorldPoint(Input.mousePosition));
    }

    public void ClearAllTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pixelgrid[x, y] == 1)
                {
                    Vector3Int currentGridPos = new Vector3Int(x, y);
                    Vector3 worldPos = grid.CellToWorld(currentGridPos);
                    Vector3Int currentTileMapPos = tilemapGrid.WorldToCell(worldPos);

                    pixelgrid[currentGridPos.x, currentGridPos.y] = 0;
                    tilemaps[currentTileMapPos.x, currentTileMapPos.y].SetTile(currentGridPos, null);
                }
            }
        }
    }

    private void Adding()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            float currentPlaceRadius = placeRadius * (1f / ppu);

            int gridRadius = Mathf.CeilToInt(currentPlaceRadius / (1f / ppu));
            float squaredRadius = Mathf.Pow(currentPlaceRadius, 2);

            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridStartpoint = grid.WorldToCell(lastMousePosition);
            Vector3Int gridEndpoint = grid.WorldToCell(mousePos);
            mousePos = grid.CellToWorld(gridEndpoint);

            List<Vector2Int> line = GenerateLine(gridStartpoint.x, gridStartpoint.y, gridEndpoint.x, gridEndpoint.y);
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

                            if (pixelgrid[currentGridPos.x, currentGridPos.y] == 1)
                                continue;

                            pixelgrid[currentGridPos.x, currentGridPos.y] = 1;
                            tilemaps[currentTileMapPos.x, currentTileMapPos.y].SetTile(currentGridPos, tile);
                        }
                    }
                }
            }

            lastMousePosition = mousePos;
        }
    }

    private void Removing()
    {
        if (Input.GetMouseButtonDown(1))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(1))
        {
            float currentPlaceRadius = placeRadius * (1f / ppu);

            int gridRadius = Mathf.CeilToInt(currentPlaceRadius / (1f / ppu));
            float squaredRadius = Mathf.Pow(currentPlaceRadius, 2);

            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridStartpoint = grid.WorldToCell(lastMousePosition);
            Vector3Int gridEndpoint = grid.WorldToCell(mousePos);
            mousePos = grid.CellToWorld(gridEndpoint);

            List<Vector2Int> line = GenerateLine(gridStartpoint.x, gridStartpoint.y, gridEndpoint.x, gridEndpoint.y);
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

                            if (pixelgrid[currentGridPos.x, currentGridPos.y] == 0)
                                continue;

                            pixelgrid[currentGridPos.x, currentGridPos.y] = 0;
                            tilemaps[currentTileMapPos.x, currentTileMapPos.y].SetTile(currentGridPos, null);
                        }
                    }
                }
            }

            lastMousePosition = mousePos;
        }
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
