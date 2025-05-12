// CorvaxNext Change Start
using Content.Shared.IgnitionSource.Components; 
using Content.Shared.Smoking;
namespace Content.Shared.Smoking.Systems;

public abstract class SharedMatchstickSystem : EntitySystem
{
    public virtual bool SetState(Entity<MatchstickComponent> ent, SmokableState state)
    {
        if (ent.Comp.CurrentState == state)
            return false;

        ent.Comp.State = state;

        Dirty(ent);
        return true;
    }
}
// CorvaxNext Change End
