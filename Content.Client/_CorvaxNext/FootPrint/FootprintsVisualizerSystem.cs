using Content.Shared._CorvaxNext.Footprint;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;

namespace Content.Client._CorvaxNext.Footprint;

public sealed class FootprintsVisualizerSystem : VisualizerSystem<FootprintComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootprintComponent, ComponentInit>(OnInitialized);
        SubscribeLocalEvent<FootprintComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInitialized(EntityUid uid, FootprintComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(FootprintVisualLayers.Print);
        UpdateAppearance(uid, comp, sprite);
    }

    private void OnShutdown(EntityUid uid, FootprintComponent comp, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(FootprintVisualLayers.Print, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void UpdateAppearance(EntityUid uid, FootprintComponent component, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(FootprintVisualLayers.Print, out var layer)
            || !TryComp<FootprintVisualizerComponent>(component.FootprintsVisualizer, out var printsComponent)
            || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (!_appearance.TryGetData<FootprintVisuals>(uid, FootprintVisualState.State, out var printVisuals, appearance))
            return;

        sprite.LayerSetState(layer, new RSI.StateId(printVisuals switch
        {
            FootprintVisuals.BareFootprint => printsComponent.RightStep ? printsComponent.RightBarePrint : printsComponent.LeftBarePrint,
            FootprintVisuals.ShoesPrint => printsComponent.ShoesPrint,
            FootprintVisuals.SuitPrint => printsComponent.SuitPrint,
            FootprintVisuals.Dragging => _random.Pick(printsComponent.DraggingPrint),
            _ => throw new ArgumentOutOfRangeException($"Unknown {printVisuals} parameter.")
        }), printsComponent.RsiPath);

        if (_appearance.TryGetData<Color>(uid, FootprintVisualState.Color, out var printColor, appearance))
            sprite.LayerSetColor(layer, printColor);
    }

    protected override void OnAppearanceChange (EntityUid uid, FootprintComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        UpdateAppearance(uid, component, sprite);
    }
}
