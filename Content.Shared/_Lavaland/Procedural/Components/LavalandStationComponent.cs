using Robust.Shared.GameStates;

namespace Content.Shared._Lavaland.Procedural.Components;

/// <summary>
/// Assigned to all main objects of the lavaland that you can FTL to.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LavalandStationComponent : Component;
