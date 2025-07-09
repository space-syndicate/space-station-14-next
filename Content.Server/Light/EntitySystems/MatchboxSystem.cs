using Content.Shared.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.IgnitionSource.Components;
using Content.Shared.IgnitionSource.EntitySystems;

namespace Content.Server.Light.EntitySystems;

/// <summary>
/// Server-side implementation of matchbox system.
/// </summary>
public sealed class MatchboxSystem : SharedMatchboxSystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Server-side subscription
        SubscribeLocalEvent<MatchboxComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(SharedStorageSystem)]);
    }
}
