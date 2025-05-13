using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StaminaDrainComponent : Component
{
    /// <summary>
    /// Количество урона наносимого стамине в секунду
    /// </summary>
    [DataField]
    public float StaminaPerSecond = 5.0f;
}