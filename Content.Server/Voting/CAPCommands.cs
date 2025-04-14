using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Discord.WebhookMessages;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Voting;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Utility;
using Robust.Server.Player;

namespace Content.Server.Voting
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class SetVotesCount : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly VoteWebhooks _voteWebhooks = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override string Command => "setvotesnumber";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            int old_votes;
            if (args.Length != 3)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 3), ("upper", 3)));
                return;
            }

            if (!int.TryParse(args[0], out int vote_id) || !int.TryParse(args[1], out int option) || !int.TryParse(args[2], out int count))
            {
                shell.WriteError("Yo, wtf?");
                return;
            }
            if (count > _playerManager.Sessions.Length)
            {
                shell.WriteError(Loc.GetString("shell-no-enought-players"));
                return;
            }
            try
            {
                old_votes = _voteManager.SetVotesCount(vote_id, option, count);
            }
            catch (ArgumentOutOfRangeException e)
            {
                shell.WriteError(e.Message);
                return;
            }
            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Corvax Antidemocraty Program started, changed in vote: {vote_id}, option: {option}, count {old_votes} => {count}");
        }
    }
}
