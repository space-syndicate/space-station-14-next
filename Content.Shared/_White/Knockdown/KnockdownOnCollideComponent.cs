namespace Content.Shared._White.Collision.Knockdown;

[RegisterComponent]
public sealed partial class KnockdownOnCollideComponent : Component
{
    [DataField]
    public bool DropItems = true;
}
