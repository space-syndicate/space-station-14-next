using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.Footprints.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FootprintComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public List<Footprint> Footprints = [];
}

[Serializable, NetSerializable]
public readonly record struct Footprint(Color Color);
