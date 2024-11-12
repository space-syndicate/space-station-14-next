using Robust.Shared.Timing;

namespace Content.Shared._CorvaxNext.Resomi.Abilities;

public abstract class SharedResomiSkillSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    public override void Initialize()
    {
    }
}
