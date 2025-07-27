using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.StatusEffect;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    private void AddCorvaxNextSmites(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        if (TryComp<StatusEffectsComponent>(args.Target, out var statusEffectsComp))
        {
            var lightningBoltName = Loc.GetString("admin-smite-lightning-bolt-name").ToLowerInvariant();
            Verb lightningBolt = new()
            {
                Text = lightningBoltName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Effects/lightning.rsi"), "lightning_2"),
                Act = () =>
                {
                    EntityManager.SpawnAtPosition("EffectFlashLightningBolt", _transformSystem.GetMoverCoordinates(args.Target));
                    _audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Effects/tesla_collapse.ogg"), args.Target, AudioParams.Default.WithVariation((float?)0.25));

                    _electrocutionSystem.TryDoElectrocution(args.Target, null, 35, TimeSpan.FromSeconds(3), refresh: true, ignoreInsulation: true);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", lightningBoltName, Loc.GetString("admin-smite-lightning-bolt-description"))
            };
            args.Verbs.Add(lightningBolt);
        }
    }
}

