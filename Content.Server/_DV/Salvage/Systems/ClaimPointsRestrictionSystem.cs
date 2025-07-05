using Content.Shared._DV.Salvage.Components;
using Content.Shared._DV.Salvage;
using Content.Shared._DV.Salvage.Systems;
using Robust.Shared.Audio;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Lathe;
using Content.Server._Lavaland.Procedural.Components;

namespace Content.Server._DV.Salvage.Systems;

public sealed class ClaimPointsRestrictionSystem : EntitySystem
{
    [Dependency] private readonly MiningPointsSystem _points = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<MiningPointsLatheComponent>(LatheUiKey.Key, subs =>
        {
            subs.Event<LatheClaimMiningPointsMessage>(OnClaimMiningPoints);
        });
    }
    private void OnClaimMiningPoints(Entity<MiningPointsLatheComponent> ent, ref LatheClaimMiningPointsMessage args)
    {
        var user = args.Actor;

        if (!(_points.TryFindIdCard(user) is { } dest))
            return;

        if (!ent.Comp.WorksOnLavaland && _entManager.TryGetComponent<TransformComponent>(ent, out var transform) && HasComp<LavalandMapComponent>(transform.MapUid))
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg"), ent);
            _popup.PopupEntity(Loc.GetString("lavaland-claim-points-restriction"), ent);

            return;
        }

        _points.TransferAll(ent.Owner, dest);
    }
}

