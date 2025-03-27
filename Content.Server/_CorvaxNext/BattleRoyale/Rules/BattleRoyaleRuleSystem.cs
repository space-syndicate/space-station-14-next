using Content.Server.Administration.Commands;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server._CorvaxNext.BattleRoyal.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Points;
using Content.Shared._CorvaxNext.Skills;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CorvaxNext.BattleRoyal.Rules;

/// <summary>
/// Battle Royale game mode where the last player standing wins
/// </summary>
public sealed class BattleRoyaleRuleSystem : GameRuleSystem<BattleRoyaleRuleComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly TransformSystem _transforms = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly KillTrackingSystem _killTracking = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSkillsSystem _skills = default!;

    // For kill callouts
    private const int MaxNormalCallouts = 60; // death-match-kill-callout-0 to death-match-kill-callout-60
    private const int MaxEnvironmentalCallouts = 10; // death-match-kill-callout-env-0 to death-match-kill-callout-env-10

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    protected override void Started(EntityUid uid, BattleRoyaleRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Check if there's only one player
        CheckLastManStanding(uid, component);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var br, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Create mind for player
            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            // Spawn player character on station
            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;
			_skills.GrantAllSkills(mob);

            // Transfer mind to created character
            _mind.TransferTo(newMind, mob);
            
            // Set outfit from BattleRoyaleRule component
            SetOutfitCommand.SetOutfit(mob, br.Gear, EntityManager);
            
            // Add kill tracking component
            EnsureComp<KillTrackerComponent>(mob);
            
            // Mark event as handled to prevent standard spawn
            ev.Handled = true;
            break;
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var br, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Check if there's only one player left alive
            CheckLastManStanding(uid, br);
        }
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var br, out var point, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Award kill points
            if (ev.Primary is KillPlayerSource player)
            {
                _point.AdjustPointValue(player.PlayerId, 1, uid, point);
            }

            // Assist points
            if (ev.Assist is KillPlayerSource assist)
            {
                _point.AdjustPointValue(assist.PlayerId, 0.5f, uid, point);
            }
            
            // Send kill callout announcement
            SendKillCallout(uid, ref ev);
        }
    }
    
    private void SendKillCallout(EntityUid uid, ref KillReportedEvent ev)
    {
        // Determine if this is an environmental death or suicide
        if (ev.Primary is KillEnvironmentSource || ev.Suicide)
        {
            var calloutNumber = _random.Next(0, MaxEnvironmentalCallouts + 1);
            var calloutId = $"death-match-kill-callout-env-{calloutNumber}";
            var victimName = GetEntityName(ev.Entity);
            
            var message = Loc.GetString(calloutId, ("victim", victimName));
            _chatManager.ChatMessageToAll(ChatChannel.Server, message, message, uid, false, true, Color.OrangeRed);
            return;
        }
        
        // Normal kill with potential assist
        string killerString;
        if (ev.Primary is KillPlayerSource primarySource)
        {
            var primaryName = GetPlayerName(primarySource.PlayerId);
            
            if (ev.Assist is KillPlayerSource assistSource)
            {
                // Kill with assist
                var assistName = GetPlayerName(assistSource.PlayerId);
                killerString = Loc.GetString("death-match-assist", 
                    ("primary", primaryName), 
                    ("secondary", assistName));
            }
            else
            {
                // Normal kill
                killerString = primaryName;
            }
            
            // Get random callout
            var calloutNumber = _random.Next(0, MaxNormalCallouts + 1);
            var calloutId = $"death-match-kill-callout-{calloutNumber}";
            var victimName = GetEntityName(ev.Entity);
            
            var message = Loc.GetString(calloutId, 
                ("killer", killerString), 
                ("victim", victimName));
            
            _chatManager.ChatMessageToAll(ChatChannel.Server, message, message, uid, false, true, Color.OrangeRed);
        }
        else if (ev.Primary is KillNpcSource npcSource)
        {
            // NPC kill
            var npcName = GetEntityName(npcSource.NpcEnt);
            killerString = npcName;
            
            // Get random callout
            var calloutNumber = _random.Next(0, MaxNormalCallouts + 1);
            var calloutId = $"death-match-kill-callout-{calloutNumber}";
            var victimName = GetEntityName(ev.Entity);
            
            var message = Loc.GetString(calloutId, 
                ("killer", killerString), 
                ("victim", victimName));
            
            _chatManager.ChatMessageToAll(ChatChannel.Server, message, message, uid, false, true, Color.OrangeRed);
        }
    }
    
    private string GetPlayerName(NetUserId userId)
    {
        if (!_player.TryGetSessionById(userId, out var session))
            return "Unknown";
            
        if (session.AttachedEntity == null)
            return session.Name;
            
        return Loc.GetString("death-match-name-player",
            ("name", MetaData(session.AttachedEntity.Value).EntityName),
            ("username", session.Name));
    }
    
    private string GetEntityName(EntityUid entity)
    {
        if (TryComp<ActorComponent>(entity, out var actor))
        {
            return Loc.GetString("death-match-name-player",
                ("name", MetaData(entity).EntityName),
                ("username", actor.PlayerSession.Name));
        }
        
        return Loc.GetString("death-match-name-npc", 
            ("name", MetaData(entity).EntityName));
    }

    private void CheckLastManStanding(EntityUid uid, BattleRoyaleRuleComponent component)
    {
        var alivePlayers = GetAlivePlayers();

        // If only one player left, they are the winner
        if (alivePlayers.Count == 1)
        {
            component.Victor = alivePlayers[0];
            
            // Make sure the player actually has a mind entity
            if (_mind.TryGetMind(component.Victor.Value, out var mindId, out var mind))
            {
                var victorName = MetaData(component.Victor.Value).EntityName;
                string playerName = mind.Session?.Name ?? victorName;
                
                // Announce the winner
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("battle-royale-winner-announcement", 
                        ("player", playerName)));
                
                // End the round after a delay
                Timer.Spawn(component.RoundEndDelay, () => {
                    if (GameTicker.RunLevel == GameRunLevel.InRound)
                        _roundEnd.EndRound();
                });
            }
        }
        // If no players left (everyone died somehow), end the round
        else if (alivePlayers.Count == 0)
        {
            component.Victor = null;
            
            // No winner - end immediately
            _roundEnd.EndRound();
        }
        // If we're starting with just one player, they win automatically
        else if (alivePlayers.Count == 1 && GameTicker.RunLevel == GameRunLevel.InRound && 
                 component.Victor == null && Timing.CurTime < TimeSpan.FromSeconds(10))
        {
            component.Victor = alivePlayers[0];
            
            if (_mind.TryGetMind(component.Victor.Value, out var mindId, out var mind))
            {
                var victorName = MetaData(component.Victor.Value).EntityName;
                string playerName = mind.Session?.Name ?? victorName;
                
                // Single player victory
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("battle-royale-single-player", 
                        ("player", playerName)));
                    
                // End round
                Timer.Spawn(component.RoundEndDelay, () => {
                    if (GameTicker.RunLevel == GameRunLevel.InRound)
                        _roundEnd.EndRound();
                });
            }
        }
    }

    private List<EntityUid> GetAlivePlayers()
    {
        var result = new List<EntityUid>();
        var mobQuery = EntityQueryEnumerator<MobStateComponent, ActorComponent>();

        while (mobQuery.MoveNext(out var uid, out var mobState, out _))
        {
            if (_mobState.IsAlive(uid, mobState))
                result.Add(uid);
        }

        return result;
    }

    protected override void AppendRoundEndText(EntityUid uid, 
        BattleRoyaleRuleComponent component, 
        GameRuleComponent gameRule, 
        ref RoundEndTextAppendEvent args)
    {
        if (!TryComp<PointManagerComponent>(uid, out var point))
            return;

        // Show the winner first if we have one
        if (component.Victor != null && _mind.TryGetMind(component.Victor.Value, out var victorMindId, out var victorMind))
        {
            var victorName = MetaData(component.Victor.Value).EntityName;
            var victorPlayerName = victorMind.Session?.Name ?? victorName;
            
            args.AddLine(Loc.GetString("battle-royale-winner", ("player", victorPlayerName)));
            args.AddLine("");
        }

        // Show the kills scoreboard
        args.AddLine(Loc.GetString("battle-royale-scoreboard-header"));
        args.AddLine(new FormattedMessage(point.Scoreboard).ToMarkup());
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var br, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            _skills.GrantAllSkills(ev.Mob);
            break;
        }
    }
}
