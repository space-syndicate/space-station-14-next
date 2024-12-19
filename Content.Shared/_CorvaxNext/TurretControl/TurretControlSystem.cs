using Content.Shared.Verbs;

namespace Content.Shared._CorvaxNext.TurretControl;

public sealed class TurretControlSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<TurretControlComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<TurretControlComponent> entity, ref GetVerbsEvent<Verb> e)
    {
        
    }
}
