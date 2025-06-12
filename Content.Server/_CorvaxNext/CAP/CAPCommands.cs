using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Discord.WebhookMessages;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Server.Player;
using System.Threading.Tasks;

namespace Content.Server.Voting
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class SetVotesCount : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly VoteWebhooks _voteWebhooks = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override string Command => "setvotescount";
        public override string Description => Loc.GetString("cmd-setvotescount-desc");
        public override string Help => Loc.GetString("cmd-setvotescount-help");


        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 0)
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-capshared-arg-vote-id"));
            }
            else if (args.Length == 1)
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-capshared-arg-option"));
            }
            else if (args.Length == 2)
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-capshared-arg-count"));
            }
            else
            {
                return CompletionResult.FromHint(Loc.GetString("shell-wrong-arguments-number"));
            }
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            int old_votes;
            if (args.Length != 3)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 3), ("correntAmount", args.Length)));
                return;
            }

            if (!int.TryParse(args[0], out int vote_id) || !int.TryParse(args[1], out int option) || !int.TryParse(args[2], out int count))
            {
                shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                return;
            }
            if (count > _playerManager.Sessions.Length)
            {
                shell.WriteError(Loc.GetString("cmd-setvotescount-no-enought-players"));
                return;
            }
            try
            {
                old_votes = _voteManager.SetVotesCount(vote_id, option, count);
                shell.WriteLine(Loc.GetString("cmd-capshared-success-msg"));
            }
            catch (ArgumentOutOfRangeException e)
            {
                shell.WriteError(e.Message);
                return;
            }
            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Corvax Antidemocraty Program started, changed in vote: {vote_id}, option: {option}, count {old_votes} => {count}");
        }
    }

    [AdminCommand(AdminFlags.Moderator)]
    public sealed class setvoteWinner : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        private readonly Random _rnd = new Random();

        public override string Command => "setvotewinner";
        public override string Description => Loc.GetString("cmd-setvotewinner-desc");
        public override string Help => Loc.GetString("cmd-setvotewinner-help");

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 0)
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-capshared-arg-vote-id"));
            }
            else if (args.Length == 1)
            {
                return CompletionResult.FromHint(Loc.GetString("cmd-capshared-arg-option"));
            }
            else
            {
                return CompletionResult.FromHint(Loc.GetString("shell-wrong-arguments-number"));
            }
        }

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            int players = _playerManager.Sessions.Length;
            List<Task> tasks = new List<Task>();
            int remainingPlayers = players;
            int players_to_option = 0;

            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("correntAmount", args.Length)));
                return;
            }

            if (!int.TryParse(args[0], out int vote_id) || !int.TryParse(args[1], out int option))
            {
                shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            try
            {
                VoteManager.VoteReg vote = _voteManager.GetVoteInfo(vote_id);

                int entries = vote.Entries.Length;

                players_to_option = (int)(players * 0.55f);
                remainingPlayers -= players_to_option;

                tasks.Add(SetVotesDelayed(vote_id, option, players_to_option));

                for (int i = 0; i < entries; i++)
                {
                    if (i == option) continue;

                    if (remainingPlayers <= 0) break;

                    int votesForOption = Math.Min(_rnd.Next(5, 8), remainingPlayers);
                    remainingPlayers -= votesForOption;

                    tasks.Add(SetVotesDelayed(vote_id, i, votesForOption));
                }

                if (remainingPlayers > 0)
                {
                    tasks.Add(SetVotesDelayed(vote_id, option,
                        _voteManager.GetVoteInfo(vote_id).Entries[option].Votes + remainingPlayers));
                }

                await Task.WhenAll(tasks);
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Corvax Antidemocraty Program started, set winner in vote: {vote_id}, option: {option}");
                shell.WriteLine(Loc.GetString("cmd-capshared-success-msg"));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (ex.ActualValue == nameof(vote_id))
                {
                    shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote-id"));
                }
                else if (ex.ActualValue == nameof(option))
                {
                    shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-option"));
                }
            }
            catch (Exception e)
            {
                shell.WriteError($"Error in vote distribution: {e.Message}");
            }
        }

        public async Task SetVotesDelayed(int vote_id, int opt, int count)
        {
            int currentVotes = _voteManager.GetVoteInfo(vote_id).Entries[opt].Votes;

            if (currentVotes < count)
            {
                for (int i = currentVotes + 1; i <= count; i++)
                {
                    _voteManager.SetVotesCount(vote_id, opt, i);
                    await Task.Delay(_rnd.Next(25, 100));
                }
            }
            else if (currentVotes > count)
            {
                for (int i = currentVotes - 1; i >= count; i--)
                {
                    _voteManager.SetVotesCount(vote_id, opt, i);
                    await Task.Delay(_rnd.Next(25, 100));
                }
            }
        }
    }
}
