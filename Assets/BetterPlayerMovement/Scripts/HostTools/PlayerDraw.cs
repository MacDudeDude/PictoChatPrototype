using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerDraw : MonoBehaviour
{
    public int currentLayer;
    public float placeRadius;

    public Vector2 playAreaBoundsX;
    public Vector2 playAreaBoundsY;

    public MouseManager mouseManager;
    public TextureManager texManager;
    public int layersAmount;
    public int collisionLayer = 1;

    public Tile[] tileValues;
    public Color32[] colorValues;

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
    private Camera cam;

    private List<int[,]> pixelgrid;
    private List<Color32[]> textureColors;

    private List<Vector3Int>[] updatedTilesPos;
    private List<TileBase>[] updatedTilesTile;
    private bool tilemapUpdated;

    // Start is called before the first frame update
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

    // Generate box colliders are around the play area, only problem is that it's too good and better than the tilemaps which creates a noticeable difference
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

    public void SetOutlineTiles()
    {
        playareaOutline = Instantiate(tilemapPrefab, Vector3.zero, Quaternion.identity, grid.transform).GetComponent<Tilemap>();

        Vector3Int[] positions = new Vector3Int[width + 2];
        TileBase[] tiles = new TileBase[width + 2];

        //Bottom outline
        for (int x = 0; x < width + 2; x++) {
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
    public void PenToolUpdate()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            
            if(mouseManager.GetObjectBetweenTwoPoints(mousePos, lastMousePosition) == null)
            {
                Vector3Int gridStartpoint = grid.WorldToCell(lastMousePosition);
                Vector3Int gridEndpoint = grid.WorldToCell(mousePos);
                DrawLine(gridStartpoint, gridEndpoint, placeRadius, 1, currentLayer);
            }

            lastMousePosition = mousePos;
        }
    }

    public void EraseToolUpdate()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridStartpoint = grid.WorldToCell(lastMousePosition);
            Vector3Int gridEndpoint = grid.WorldToCell(mousePos);

            DrawLine(gridStartpoint, gridEndpoint, placeRadius, 0, currentLayer);

            lastMousePosition = mousePos;
        }
    }

    private void FixedUpdate()
    {
        if (tilemapUpdated)
        {
            UpdateTiles();
        }
    }

    public void UpdateTiles()
    {
        for (int i = 0; i < updatedTilesPos.Length; i++)
        {
            if(updatedTilesPos[i].Count > 0)
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

    private void QueTile(int x, int y, Vector3Int pos, Tile tile, int value, int layer)
    {
        int i = To1DIndex(x, y);

        if(layer == collisionLayer)
        {
            updatedTilesPos[i].Add(pos);
            updatedTilesTile[i].Add(tile);
        }

        pixelgrid[layer][pos.x, pos.y] = value;

        i = (pos.y * width) + pos.x;
        textureColors[layer][i] = colorValues[value];

        tilemapUpdated = true;
    }

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

    public int To1DIndex(int x, int y)
    {
        return y * rows + x;
    }

    public (int x, int y) To2DIndex(int i)
    {
        int x = i % rows;
        int y = i / rows;
        return (x, y);
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
