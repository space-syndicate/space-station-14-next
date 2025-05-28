using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ScannerGate
{
    [NetworkedComponent, RegisterComponent, AutoGenerateComponentPause]
    public sealed partial class ScannerGateComponent : Component
    {
        [DataField]
        public EntityWhitelist? Blacklist;

        [DataField("whitelist")]
        public EntityWhitelist? GrantIgnoreWhitelist;

        [DataField, AutoPausedField]
        public TimeSpan NextCheck = TimeSpan.Zero;

        [DataField]
        public TimeSpan CheckInterval = TimeSpan.FromSeconds(2.5);

        [DataField(required: true)]
        public string FixtureId = string.Empty;

        [DataField]
        public SoundSpecifier CheckPassedSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

        [DataField]
        public SoundSpecifier CheckDeniedSound = new SoundPathSpecifier("/Audio/_CorvaxNext/Misc/scanbuzz.ogg");

        // States
        [DataField]
        public bool Alarming = false;

        [DataField]
        public bool Passing = false;

        [DataField]
        public bool Enabled = false;

        // Visual
        [DataField]
        public string VisualStateIdle = "scangate_idle";

        [DataField]
        public string VisualStatePassed = "scangate_ok";

        [DataField]
        public string VisualStateDenied = "scangate_no";
    }

    [Serializable, NetSerializable]
    public enum ScannerGateVisualLayers : byte
    {
        Base,
        Status
    }

    [Serializable, NetSerializable]
    public enum ScannerGateStatusVisualState : byte
    {
        Idle,
        Passed,
        Denied,
        Off
    }
}
