using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class PlayerController : MonoBehaviour
{
    [Header("Map Reference")]
    [SerializeField] private MapGenerator _mapGenerator;

    [Header("Entity Tilemap & Player Tile")]
    [Tooltip("Tilemap used to draw the player (e.g. Tilemap_Entities).")]
    [SerializeField] private Tilemap _entityTilemap;

    [Tooltip("Tile asset for the player from your tileset/palette.")]
    [SerializeField] private TileBase _playerTile;

    [Header("Movement")]
    [Tooltip("Seconds between moves when holding a direction.")]
    [SerializeField] private float _moveDelay = 0.15f;

    [Header("Player Appearance")]
    [SerializeField] private Color _playerColor = Color.white;


    private Map _map;
    private Player _player;
    private float _moveCooldown;

    private void Start()
    {
        if (_mapGenerator == null)
        {
            Debug.LogError("PlayerController: MapGenerator reference is not set.");
            enabled = false;
            return;
        }

        if (_entityTilemap == null)
        {
            Debug.LogError("PlayerController: Entity Tilemap is not set.");
            enabled = false;
            return;
        }

        if (_playerTile == null)
        {
            Debug.LogError("PlayerController: Player Tile is not set.");
            enabled = false;
            return;
        }

        _map = _mapGenerator.CurrentMap;
        if (_map == null)
        {
            Debug.LogError("PlayerController: Map is null. Make sure MapGenerator has generated a map before Player starts.");
            enabled = false;
            return;
        }

        // Find a starting walkable tile near the center
        int startX = _map.Width  / 2;
        int startY = _map.Height / 2;

        if (!_map.IsWalkable(startX, startY))
        {
            bool found = false;
            for (int radius = 1; radius < Mathf.Max(_map.Width, _map.Height); radius++)
            {
                for (int x = startX - radius; x <= startX + radius; x++)
                for (int y = startY - radius; y <= startY + radius; y++)
                {
                    if (!_map.InBounds(x, y)) continue;
                    if (_map.IsWalkable(x, y))
                    {
                        startX = x;
                        startY = y;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }

        _player = new Player(startX, startY);
        RedrawPlayerTile();
    }

private void Update()
{
    if (_map == null || _player == null)
        return;

    _moveCooldown -= Time.deltaTime;
    if (_moveCooldown > 0f)
        return;

    int dx = 0;
    int dy = 0;

    bool shift =
        Input.GetKey(KeyCode.LeftShift) ||
        Input.GetKey(KeyCode.RightShift);

    if (!shift)
    {
        // Cardinal movement only
        if (Input.GetKey(KeyCode.UpArrow))
        {
            dy = 1;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            dy = -1;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            dx = 1;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            dx = -1;
        }
    }
    else
    {
        // Diagonal movement only, using Shift + one arrow
        if (Input.GetKey(KeyCode.UpArrow))
        {
            dx = -1;
            dy = 1;   // up-left
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            dx = 1;
            dy = 1;   // up-right
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            dx = 1;
            dy = -1;  // down-right
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            dx = -1;
            dy = -1;  // down-left
        }
    }

    // No input this frame
    if (dx == 0 && dy == 0)
        return;

    TryMove(dx, dy);
    _moveCooldown = _moveDelay;
}

    private void TryMove(int dx, int dy)
    {
        int targetX = _player.X + dx;
        int targetY = _player.Y + dy;

        if (!_map.InBounds(targetX, targetY))
            return;

        if (!_player.CanEnterTile(_map, targetX, targetY))
            return;

        _player.SetPosition(targetX, targetY);
        RedrawPlayerTile();
    }

    /// <summary>
    /// Clears old player tile and sets a new one at the player's grid position.
    /// This ensures player is always exactly on a tile, never in-between.
    /// </summary>
private void RedrawPlayerTile()
{
    // Clear previous player tile
    _entityTilemap.ClearAllTiles();

    Vector3Int cellPos = new Vector3Int(_player.X, _player.Y, 0);

    // Set the tile
    _entityTilemap.SetTile(cellPos, _playerTile);

    // 👉 Apply color tint
    _entityTilemap.SetColor(cellPos, _playerColor);

    // Keep transform aligned (optional if used for camera follow)
    transform.position = _entityTilemap.CellToWorld(cellPos);
}

}

