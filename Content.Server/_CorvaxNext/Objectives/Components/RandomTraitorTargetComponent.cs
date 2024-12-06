using Content.Server.Objectives.Components;

namespace Content.Server._CorvaxNext.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="KeepAliveConditionComponent"/>
/// to protect a player that is targeted to kill by another traitor
/// </summary>
[RegisterComponent]
public sealed partial class RandomTraitorTargetComponent : Component
{
}
