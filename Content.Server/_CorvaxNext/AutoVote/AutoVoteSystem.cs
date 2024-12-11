using Robust.Shared.Configuration;
using Content.Server.Voting.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Content.Shared._CorvaxNext.NextVars;
using Robust.Server.Player;
using Content.Server.GameTicking;

namespace Content.Server._CorvaxNext.AutoVote;

public sealed class AutoVoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] public readonly IVoteManager _voteManager = default!;
    [Dependency] public readonly IPlayerManager _playerManager = default!;

    public bool _shouldVoteNextJoin = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnReturnedToLobby);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    public void OnReturnedToLobby(RoundRestartCleanupEvent ev) => CallAutovote();

    public void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        if (!_shouldVoteNextJoin)
            return;

        CallAutovote();
        _shouldVoteNextJoin = false;
    }

    private void CallAutovote()
    {
        if (!_cfg.GetCVar(NextVars.AutoVoteEnabled))
            return;

        if (_playerManager.PlayerCount == 0)
        {
            _shouldVoteNextJoin = true;
            return;
        }

        if (_cfg.GetCVar(NextVars.MapAutoVoteEnabled))
            _voteManager.CreateStandardVote(null, StandardVoteType.Map);
        if (_cfg.GetCVar(NextVars.PresetAutoVoteEnabled))
            _voteManager.CreateStandardVote(null, StandardVoteType.Preset);
    }
}
