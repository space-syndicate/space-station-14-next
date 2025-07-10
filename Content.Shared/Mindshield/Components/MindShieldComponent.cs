using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;   // CorvaxNext - MindShieldAlert

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// If a player has a Mindshield they will get this component to prevent conversion.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class MindShieldComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";
    // CorvaxNext - MindShieldAlert
    [DataField]
    public ProtoId<AlertPrototype> MindShieldAlert = "MindShieldAlert";
}
