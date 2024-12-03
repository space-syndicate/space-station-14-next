using Content.Shared._CorvaxNext.Footprints;
using Content.Shared._CorvaxNext.Footprints.Components;
using Robust.Client.GameObjects;

namespace Content.Client._CorvaxNext.Footprints;

public sealed class FootprintSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FootprintComponent, ComponentStartup>(OnComponentStartup);
        SubscribeNetworkEvent<FootprintChangedEvent>(OnFootprintChanged);
    }

    private void OnComponentStartup(Entity<FootprintComponent> entity, ref ComponentStartup e)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        foreach (var footprint in entity.Comp.Footprints)
            sprite.AddLayer(new PrototypeLayerData()
            {
                RsiPath = "/Textures/Clothing/Shoes/color.rsi",
                Color = footprint.Color
            });
    }

    private void OnFootprintChanged(FootprintChangedEvent e)
    {
        var entity = GetEntity(e.Entity);

        if (!TryComp<FootprintComponent>(entity, out var footprint))
            return;

        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        if (footprint.Footprints.Count < 1)
            return;

        sprite.AddLayer(new PrototypeLayerData()
        {
            RsiPath = "/Textures/Clothing/Shoes/color.rsi",
            Color = footprint.Footprints[^1].Color
        });
    }
}
