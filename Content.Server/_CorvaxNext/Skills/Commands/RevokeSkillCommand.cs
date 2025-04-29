using System.Linq;
using Content.Server.Administration;
using Content.Shared._CorvaxNext.Skills;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CorvaxNext.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class RevokeSkillCommand : IConsoleCommand
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public string Command => "revokeskill";

    public string Description => "Revokes skill from given entity.";

    public string Help => "revokeskill <entityuid> <skill>";

    public void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(_localization.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var id))
        {
            shell.WriteError(_localization.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entity.TryGetEntity(id, out var entity))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!Enum.TryParse<Shared._CorvaxNext.Skills.Skills>(args[1], out var skill))
        {
            shell.WriteError("No such skill.");
            return;
        }

        if (!_entity.TryGetComponent<SkillsComponent>(entity.Value, out var component))
            return;

        component.Skills.Remove(skill);

        _entity.Dirty(entity.Value, component);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 2)
        {
            var component = int.TryParse(args[0], out var id)
                ? _entity.TryGetEntity(new(id), out var entity)
                    ? _entity.TryGetComponent<SkillsComponent>(entity, out var comp)
                        ? comp
                        : null
                    : null
                : null;

            return CompletionResult.FromOptions(Enum.GetValues<Shared._CorvaxNext.Skills.Skills>()
                .Where(value => component?.Skills.Contains(value) != false)
                .Select(value => value.ToString())
                .Where(name => name.ToString().StartsWith(args[1], true, null))
                .Select(value => new CompletionOption(value.ToString())));
        }

        return CompletionResult.Empty;
    }
}
