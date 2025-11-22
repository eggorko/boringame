// Player.cs
// Logical player entity, can be extended with HP, inventory, etc.

public sealed class Player : Entity
{
    public Player(int x, int y) : base(x, y)
    {
    }

    // In future, override CanEnterTile if player has unique movement rules.
    // public override bool CanEnterTile(Map map, int x, int y) { ... }
}
