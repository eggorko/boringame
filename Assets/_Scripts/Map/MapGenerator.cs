// MapGenerator.cs
// Attach this to a GameObject that has a Tilemap component (under a Grid).
// It will generate a simple map and render it using tiles set in the Inspector.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TileVisual
{
    public TileType TileType;
    public TileBase Tile;
}

[RequireComponent(typeof(Tilemap))]
public sealed class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int _width  = 32;
    [SerializeField] private int _height = 18;

    [Tooltip("Chance for a cell (non-border) to become a wall, 0-1.")]
    [Range(0f, 1f)]
    [SerializeField] private float _randomWallChance = 0.15f;

    [Header("Tile Visuals")]
    [Tooltip("Assign Floor and Wall tile assets here.")]
    [SerializeField] private List<TileVisual> _tileVisuals = new();

    private Tilemap _tilemap;
    private Map _map;
    public Map CurrentMap => _map;   // public getter

    private Dictionary<TileType, TileBase> _tileLookup;

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        BuildTileLookup();
    }

    private void Start()
    {
        GenerateAndRender();
        //CenterCamera(_map);
    }

    /// <summary>
    /// Rebuilds the dictionary from TileType to TileBase
    /// so we can do O(1) lookups when rendering.
    /// </summary>
    private void BuildTileLookup()
    {
        _tileLookup = new Dictionary<TileType, TileBase>();

        foreach (var tv in _tileVisuals)
        {
            if (!_tileLookup.ContainsKey(tv.TileType))
            {
                _tileLookup.Add(tv.TileType, tv.Tile);
            }
            else
            {
                Debug.LogWarning($"Duplicate TileVisual for TileType {tv.TileType} on {name}.");
            }
        }
    }

    /// <summary>
    /// Public method so you can regenerate from a button / editor, if you want.
    /// </summary>
    [ContextMenu("Generate And Render Map")]
    public void GenerateAndRender()
    {
        _map = GenerateValidMap(_width, _height);
        RenderMap(_map);
    }

    /// <summary>
    /// Very simple generation:
    /// - Borders are walls.
    /// - Interior is mostly floor with some random walls.
    /// Replace this later with rooms, corridors, etc.
    /// </summary>
    private Map GenerateMap(int width, int height)
    {
        var map = new Map(width, height, TileType.Floor);

        var rnd = new System.Random();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            bool isBorder =
                x == 0 || y == 0 ||
                x == width - 1 ||
                y == height - 1;

            if (isBorder)
            {
                map.SetTile(x, y, TileType.Wall);
            }
            else
            {
                // Randomly scatter some walls inside.
                // You can tweak/remove this later.
                if (rnd.NextDouble() < _randomWallChance)
                    map.SetTile(x, y, TileType.Wall);
                else
                    map.SetTile(x, y, TileType.Floor);
            }
        }

        return map;
    }

    /// <summary>
    /// Pushes the Map data into the Unity Tilemap for visual representation.
    /// </summary>
private void RenderMap(Map map)
{
    if (_tileLookup == null || _tileLookup.Count == 0)
    {
        BuildTileLookup();
    }

    _tilemap.ClearAllTiles();

    for (int x = 0; x < map.Width; x++)
    for (int y = 0; y < map.Height; y++)
    {
        var type = map.GetTile(x, y);

        if (!_tileLookup.TryGetValue(type, out var tileBase))
        {
            Debug.LogWarning($"No TileBase assigned for TileType {type} on {name}.");
            continue;
        }

        var pos = new Vector3Int(x, y, 0);
        _tilemap.SetTile(pos, tileBase);
    }

    // ============================
    // 🔥 CENTER THE TILEMAP HERE 🔥
    // ============================
    // Moves the Tilemap so the grid center aligns with world (0,0)
    CenterCameraOnMap(map);

}
private void CenterCameraOnMap(Map map)
{
    var cam = Camera.main;
    if (cam == null)
        return;

    // Center on the middle of the grid
    cam.transform.position = new Vector3(
        map.Width  / 2f,
        map.Height / 2f,
        cam.transform.position.z  // keep existing Z (e.g. -10)
    );

    // Optional: adjust size so the whole map fits in view
    if (cam.orthographic)
    {
        // Half-height is Height/2; half-width is Width/2.
        // OrthographicSize is half-height in world units.
        float halfHeight = map.Height / 2f;
        float halfWidth  = map.Width  / 2f;

        // To fit width, size must be at least halfWidth / aspect.
        float sizeForWidth = halfWidth / cam.aspect;

        cam.orthographicSize = Mathf.Max(halfHeight, sizeForWidth);
    }
}

private Map GenerateValidMap(int width, int height, int maxAttempts = 20)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        var candidate = GenerateMap(width, height);

        if (IsMapFullyAccessible(candidate))
        {
            // Debug.Log($"Generated valid map in {attempt} attempt(s).");
            return candidate;
        }
    }

    Debug.LogWarning($"Failed to generate fully accessible map after {maxAttempts} attempts. Returning last candidate.");
    return GenerateMap(width, height);
}

private bool IsMapFullyAccessible(Map map)
{
    int width  = map.Width;
    int height = map.Height;

    // 1. Count all walkable tiles and find a starting walkable tile
    int totalWalkable = 0;
    int startX = -1;
    int startY = -1;

    for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
    {
        if (map.IsWalkable(x, y))
        {
            totalWalkable++;

            if (startX == -1)
            {
                startX = x;
                startY = y;
            }
        }
    }

    // No walkable tiles at all → consider this "not accessible"
    if (totalWalkable == 0)
        return false;

    // 2. Flood fill (BFS) from the starting walkable tile
    var visited = new bool[width, height];
    var queue = new Queue<(int x, int y)>();

    visited[startX, startY] = true;
    queue.Enqueue((startX, startY));

    int visitedWalkable = 0;

    // 4-directional neighbours
    int[] neighborOffsetsX = { 1, -1, 0, 0 };
    int[] neighborOffsetsY = { 0, 0, 1, -1 };

    while (queue.Count > 0)
    {
        var (cx, cy) = queue.Dequeue();
        visitedWalkable++;

        for (int i = 0; i < 4; i++)
        {
            int nx = cx + neighborOffsetsX[i];
            int ny = cy + neighborOffsetsY[i];

            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                continue;

            if (visited[nx, ny])
                continue;

            if (!map.IsWalkable(nx, ny))
                continue;

            visited[nx, ny] = true;
            queue.Enqueue((nx, ny));
        }
    }

    // 3. If the number of walkable tiles visited by flood fill
    //    equals the total walkable tiles on the map -> fully connected.
    bool fullyAccessible = (visitedWalkable == totalWalkable);

    // Optional debug log:
    // Debug.Log($"Accessible walkable tiles: {visitedWalkable}/{totalWalkable}. Fully accessible: {fullyAccessible}");

    return fullyAccessible;
}


}
