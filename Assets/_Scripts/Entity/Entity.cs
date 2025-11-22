// Entity.cs
// Pure domain entity. No UnityEngine here.

public abstract class Entity
{
    public int X { get; protected set; }
    public int Y { get; protected set; }

    protected Entity(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Logical position setter. Does not move any Unity transforms.
    /// </summary>
    public virtual void SetPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Can this entity enter the given tile?
    /// For now we just ask the map if it's walkable.
    /// Later you can override this per-entity (flying, swimming, etc.).
    /// </summary>
    public virtual bool CanEnterTile(Map map, int x, int y)
    {
        return map.IsWalkable(x, y);
    }
}
