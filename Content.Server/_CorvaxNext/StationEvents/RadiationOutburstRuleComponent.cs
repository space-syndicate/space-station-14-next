using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RadiationOutburstRuleComponent))]
public sealed partial class RadiationOutburstRuleComponent : Component
{
    [DataField]
    public int severity = 5;

    [DataField]
    public int maxSeverity = 5;
}
