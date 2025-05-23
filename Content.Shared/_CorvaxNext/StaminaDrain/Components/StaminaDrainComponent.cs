using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxNext.StaminaDrain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StaminaDrainComponent : Component
{
    [DataField]
    public float StaminaPerSecond = 5.0f;
}