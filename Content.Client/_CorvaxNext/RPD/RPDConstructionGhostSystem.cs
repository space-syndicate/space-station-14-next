using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.RPD;
using Content.Shared.RPD.Components;
using Content.Shared.RPD.Systems;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;


namespace Content.Client.RPD;

public sealed class RPDConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RPDSystem _rpdSystem = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;

    private string _placementMode = typeof(AlignRPDConstruction).Name;
    private Direction _placementDirection = default;
    private bool _useMirrorPrototype = false;
    public event EventHandler? FlipConstructionPrototype;

    public override void Initialize()
    {
        base.Initialize();

        // bind key
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorFlipObject,
                new PointerInputCmdHandler(HandleFlip, outsidePrediction: true))
            .Register<RPDConstructionGhostSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<RPDConstructionGhostSystem>();
        base.Shutdown();
    }

    private bool HandleFlip(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State == BoundKeyState.Down)
        {
            if (!_placementManager.IsActive || _placementManager.Eraser)
                return false;

            var placerEntity = _placementManager.CurrentPermission?.MobUid;

            if(!TryComp<RPDComponent>(placerEntity, out var rpd) ||
                string.IsNullOrEmpty(rpd.CachedPrototype.MirrorPrototype))
                return false;

            _useMirrorPrototype = !rpd.UseMirrorPrototype;

            var useProto = _useMirrorPrototype ? rpd.CachedPrototype.MirrorPrototype : rpd.CachedPrototype.Prototype;
            CreatePlacer(placerEntity.Value, rpd, useProto);

            // tell the server

            RaiseNetworkEvent(new RPDConstructionGhostFlipEvent(GetNetEntity(placerEntity.Value), _useMirrorPrototype));
        }

        return true;
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRPD = HasComp<RPDComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RCD (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRPD))
            return;

        // Determine if player is carrying an RCD in their active hand
        var player = _playerManager.LocalSession?.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands))
            return;

        var heldEntity = hands.ActiveHand?.HeldEntity;

        if (!TryComp<RPDComponent>(heldEntity, out var rpd))
        {
            // If the player was holding an RCD, but is no longer, cancel placement
            if (placerIsRPD)
                _placementManager.Clear();

            return;
        }

        // Update the direction the RCD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new RPDConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        // If the placer has not changed build it.
        _rpdSystem.UpdateCachedPrototype(heldEntity.Value, rpd);
        var useProto = (_useMirrorPrototype && !string.IsNullOrEmpty(rpd.CachedPrototype.MirrorPrototype)) ? rpd.CachedPrototype.MirrorPrototype : rpd.CachedPrototype.Prototype;

        if (heldEntity != placerEntity || useProto != placerProto)
        {
            CreatePlacer(heldEntity.Value, rpd, useProto);
        }


    }

    private void CreatePlacer(EntityUid uid, RPDComponent component, string? prototype)
    {
        // Create a new placer
        var newObjInfo = new PlacementInformation
        {
            MobUid = uid,
            PlacementOption = _placementMode,
            EntityType = prototype,
            Range = (int) Math.Ceiling(SharedInteractionSystem.InteractionRange),
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
