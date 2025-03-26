using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Pinpointer;

namespace Content.Shared._CorvaxNext.BattleRoyale.RangeFinder;

/// <summary>
/// Компонент для отображения направления к центру сужающегося круга в режиме Battle Royale
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedRangeFinderSystem))]
public sealed partial class RangeFinderComponent : Component
{
    /// <summary>
    /// Расстояние считается средним на этом значении
    /// </summary>
    [DataField("mediumDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float MediumDistance = 16f;

    /// <summary>
    /// Расстояние считается близким на этом значении
    /// </summary>
    [DataField("closeDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float CloseDistance = 8f;

    /// <summary>
    /// Расстояние считается достигнутым на этом значении
    /// </summary>
    [DataField("reachedDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float ReachedDistance = 1f;

    /// <summary>
    /// Точность указателя в радианах
    /// </summary>
    [DataField("precision"), ViewVariables(VVAccess.ReadWrite)]
    public double Precision = 0.09;

    [ViewVariables, AutoNetworkedField]
    public bool IsActive = false;

    [ViewVariables, AutoNetworkedField]
    public Angle ArrowAngle;

    [ViewVariables, AutoNetworkedField]
    public Distance DistanceToTarget = Distance.Unknown;

    /// <summary>
    /// Отслеживаемый DynamicRange
    /// </summary>
    [ViewVariables]
    public EntityUid? TargetRange = null;
}
