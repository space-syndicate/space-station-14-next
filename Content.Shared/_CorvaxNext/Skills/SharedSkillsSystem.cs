namespace Content.Shared._CorvaxNext.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    public bool HasSkill(EntityUid entity, Skills skill, SkillsComponent? component = null)
    {
        if (!Resolve(entity, ref component, false))
            return false;

        return component.Skills.Contains(skill);
    }

    public void GrantAllSkills(EntityUid entity, SkillsComponent? component = null)
    {
        component ??= EnsureComp<SkillsComponent>(entity);

        component.Skills.UnionWith(Enum.GetValues<Skills>());

        Dirty(entity, component);
    }
}
