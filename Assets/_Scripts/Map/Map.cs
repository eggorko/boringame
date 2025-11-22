// Map.cs
// Pure data model for your roguelike map.
// No UnityEngine references here so you can test / extend it easily.

public enum TileType
{
    Floor = 0,
    Wall  = 1,
    // Later: Water, Lava, Pit, etc.
}

public sealed class Map
{
    public int Width  { get; }
    public int Height { get; }

    private readonly TileType[,] _tiles;

    public Map(int width, int height, TileType defaultTile = TileType.Floor)
    {
        if (width <= 0)  throw new System.ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new System.ArgumentOutOfRangeException(nameof(height));

        Width  = width;
        Height = height;
        _tiles = new TileType[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            _tiles[x, y] = defaultTile;
        }
    }

    /// <summary>
    /// Returns true if (x,y) is within the map bounds.
    /// </summary>
    public bool InBounds(int x, int y)
        => x >= 0 && y >= 0 && x < Width && y < Height;

    public TileType GetTile(int x, int y)
    {
        if (!InBounds(x, y))
            throw new System.ArgumentOutOfRangeException($"({x},{y}) is outside map bounds");

        return _tiles[x, y];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (!InBounds(x, y))
            throw new System.ArgumentOutOfRangeException($"({x},{y}) is outside map bounds");

        _tiles[x, y] = type;
    }

    /// <summary>
    /// Basic walkability rule for now:
    /// Floor = walkable, Wall = not.
    /// Later you can expand this or move it into a movement service.
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        return _tiles[x, y] == TileType.Floor;
    }
}
