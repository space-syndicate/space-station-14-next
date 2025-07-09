using Robust.Shared.GameStates;

namespace Content.Shared._Lavaland.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the firerate of a gun.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunUpgradeSystem))]
public sealed partial class LavalandGunUpgradeFireRateComponent : Component
{
    [DataField]
    public float Coefficient = 1;
}
