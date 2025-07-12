using Content.Client._Lavaland.Procedural.Components;
using Content.Shared._Lavaland.Procedural.Components;
using Content.Shared.Random.Rules;

namespace Content.Client._CorvaxNext.Random.Rules;

public sealed partial class OnLavalandRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent(uid, out TransformComponent? xform))
            return false;

        if (!entManager.HasComponent<LavalandMapComponent>(xform.MapUid) || entManager.HasComponent<LavalandStationComponent>(xform.GridUid))
        {
            return Inverted;
        }

        return !Inverted;
    }
}
