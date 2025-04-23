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

    [AdminCommand(AdminFlags.Moderator)]
    public sealed class MakeVoteWinner : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        private readonly Random _rnd = new Random();
        public override string Command => "setvotewinner";
        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            int players = _playerManager.Sessions.Count;
            int entries = 0;
            List<Task> tasks = new List<Task>();

            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 2), ("upper", 2)));
                return;
            }

            if (!int.TryParse(args[0], out int vote_id) || !int.TryParse(args[1], out int option))
            {
                shell.WriteError("Yo, wtf?");
                return;
            }

            try
            {
                VoteManager.VoteReg vote = _voteManager.GetVoteInfo(vote_id);

                entries = vote.Entries.Length;
                int remainingPlayers = players;

                int players_to_option = (int)(players * 0.55f);
                remainingPlayers -= players_to_option;

                Console.WriteLine($"Assigning {players_to_option} votes to option {option}, {remainingPlayers} players remain");
                tasks.Add(SetVotesDelayed(vote_id, option, players_to_option));


                for (int i = 0; i < entries; i++)
                {
                    if (i == option) continue;

                    if (remainingPlayers <= 0) break;

                    int votesForOption = Math.Min(_rnd.Next(5, 8), remainingPlayers);
                    remainingPlayers -= votesForOption;

                    Console.WriteLine($"Assigning {votesForOption} votes to option {i}, {remainingPlayers} players remain");
                    tasks.Add(SetVotesDelayed(vote_id, i, votesForOption));
                }

                if (remainingPlayers > 0)
                {
                    Console.WriteLine($"Assigning remaining {remainingPlayers} votes to option {option}");
                    tasks.Add(SetVotesDelayed(vote_id, option,
                        _voteManager.GetVoteInfo(vote_id).Entries[option].Votes + remainingPlayers));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                shell.WriteError($"Error in vote distribution: {e.Message}");
            }
        }

        public async Task SetVotesDelayed(int vote_id, int opt, int count)
        {
            try
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

                Console.WriteLine($"value ({opt}): {currentVotes}==>{_voteManager.GetVoteInfo(vote_id).Entries[opt].Votes}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating votes: {ex.Message}");
                throw;
            }
        }
    }
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class GetActiveVotes : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;

        public override string Command => "activevoteinfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (VoteManager.VoteReg vote in _voteManager.GetActiveVotes())
            {
                shell.WriteLine($"Vote {vote.Title} with id: {vote.Id}");
            }
        }
    }
}
