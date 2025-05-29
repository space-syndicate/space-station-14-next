
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Content.Server._CorvaxNext.Botany.Systems;
namespace Content.Server._CorvaxNext.Botany.Components;
/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, Access(typeof(SeedLungSystem))]
public sealed partial class SeedLungComponent : Component
{
    [DataField]
    [Access(typeof(SeedLungSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public GasMixture Air = new()
    {
        Volume = 6,
        Temperature = Atmospherics.NormalBodyTemperature
    };

    /// <summary>
    /// The name/key of the solution on this entity which these lungs act on.
    /// </summary>
    [DataField]
    public string SolutionName = SeedLungSystem.SeedLungSoluotionName;

    /// <summary>
    /// The solution on this entity that these lungs act on.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

}

