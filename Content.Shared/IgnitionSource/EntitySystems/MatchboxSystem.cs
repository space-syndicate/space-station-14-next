using Content.Shared.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.IgnitionSource.Components;

namespace Content.Shared.IgnitionSource.EntitySystems;

/// <summary>
/// Handling shared logic for matchboxes.
/// </summary>
public abstract class SharedMatchboxSystem : EntitySystem
{
    [Dependency] protected readonly MatchstickSystem Matchstick = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Event subscription moved to derived classes to avoid duplication
    }

    /// <summary>
    /// Try to ignite a matchstick using a matchbox.
    /// </summary>
    protected void OnInteractUsing(Entity<MatchboxComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<MatchstickComponent>(args.Used, out var matchstick))
            return;

        args.Handled = Matchstick.TryIgnite((args.Used, matchstick), args.User);
    }
}
