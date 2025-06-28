using Content.Shared._CorvaxNext.Targeting;
using Content.Shared.Body.Components;
using Robust.Shared.Serialization;
using Content.Shared.FixedPoint; // CorvaxNext: healthAnalyzerupdate

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;
    public Dictionary<TargetBodyPart, TargetIntegrity>? Body; // CorvaxNext: surgery
    public NetEntity? Part; // CorvaxNext: surgery
    public readonly Dictionary<string, FixedPoint2>? Chemicals; // CorvaxNext: healthAnalyzerupdate

    public HealthAnalyzerScannedUserMessage(NetEntity? targetEntity, float temperature, float bloodLevel, bool? scanMode, bool? bleeding, bool? unrevivable, Dictionary<TargetBodyPart, TargetIntegrity>? body, NetEntity? part = null, Dictionary<string, FixedPoint2>? сhemicals = null) // ,Dictionary<string, FixedPoint2>? chemicals = null - CorvaxNext
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        Body = body; // CorvaxNext: surgery
        Part = part; // CorvaxNext: surgery
        Chemicals = сhemicals; // CorvaxNext: healthAnalyzerupdate
    }
}

// start-_CorvaxNext: surgery
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPartMessage(NetEntity? owner, TargetBodyPart? bodyPart) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Owner = owner;
    public readonly TargetBodyPart? BodyPart = bodyPart;

}
// end-_CorvaxNext: surgery
