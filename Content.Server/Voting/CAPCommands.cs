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
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            int players = 100;//_playerManager.Sessions.Length;
            int entries = 0;
            int players_to_option = 0;
            Task[] tasks = new Task[10];

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
                players_to_option = _rnd.Next((int)(players * .45f), (int)(players * .6f));

                Console.WriteLine($"set {players_to_option} votes to {option}'th option, {players} players remain");
                tasks[option] = Task.Run(async () => await SetVotesDelayed(vote_id, option, players_to_option));

                for (int i = 0; i < entries; i++)
                {
                    players_to_option = _rnd.Next(1, 8);

                    if (option == i)
                        continue;

                    players -= players_to_option;

                    Console.WriteLine($"setting {players_to_option} votes to {i}'th option, {players} players remain");
                    tasks[i] = Task.Run(async () => await SetVotesDelayed(vote_id, i, players_to_option));
                }
                Task.WaitAll(tasks[..(entries - 1)]);
            }
            catch (ArgumentOutOfRangeException e)
            {
                shell.WriteError(e.Message);
                return;
            }
        }

        public async Task SetVotesDelayed(int vote_id, int option, int count)
        {
            Random rnd = new Random();
            for (int i = 0; i <= count; i++)
            {


                _voteManager.SetVotesCount(vote_id, option, i);
                await Task.Delay(rnd.Next(10, 100));
            }
            Console.WriteLine($"set {count} votes to {option}'th option");
        }
    }
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class GetActiveVotes : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;

        public override string Command => "active_vote_info";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (VoteManager.VoteReg vote in _voteManager.GetActiveVotes())
            {
                shell.WriteLine($"Vote {vote.Title} with id: {vote.Id}");
            }
        }
    }
}
