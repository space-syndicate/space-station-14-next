using Content.Server.Administration.Commands;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Server._CorvaxNext.BattleRoyal.Rules.Components;
using Content.Server._CorvaxNext.Ghostbar.Components;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Points;
using Content.Shared._CorvaxNext.Skills;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Enums;

namespace Content.Server._CorvaxNext.BattleRoyal.Rules;

/// <summary>
///     Battle Royale game mode where the last player standing wins,
///     со встроенными проверками для запрета позднего входа.
/// </summary>
public sealed class BattleRoyaleRuleSystem : GameRuleSystem<BattleRoyaleRuleComponent>
{
    // Оригинальные зависимости
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

    // Новая зависимость для работы с системой прибытия из дополнений
    [Dependency] private readonly ArrivalsSystem _arrivals = default!;

    // Для kill callouts
    private const int MaxNormalCallouts = 60;  // death-match-kill-callout-0..60
    private const int MaxEnvironmentalCallouts = 10; // death-match-kill-callout-env-0..10

    public override void Initialize()
    {
        base.Initialize();

        // Оригинальные подписки на события
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        // Подписки для контроля входа и системы прибытия
        SubscribeLocalEvent<RefreshLateJoinAllowedEvent>(OnRefreshLateJoinAllowed);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(ArrivalsSystem) });

        // Добавляем подписку на событие отключения игрока
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    /// <summary>
    ///     Запрещаем поздний вход, если режим Battle Royale активен.
    /// </summary>
    private void OnRefreshLateJoinAllowed(RefreshLateJoinAllowedEvent ev)
    {
        if (CheckBattleRoyaleActive())
        {
            ev.Disallow();
        }
    }

    /// <summary>
    ///     Перехватываем событие спавна игрока для блокировки системы прибытия.
    ///     Это работает как для позднего входа, так и для первого присоединения.
    /// </summary>
    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        // Если режим Battle Royale активен и система прибытия пытается обработать спавн,
        // то не позволяем ей это сделать, отмечая результат как null
        if (CheckBattleRoyaleActive() && ev.SpawnResult == null)
        {
            // Проверяем, есть ли в этом событии компонент StationArrivalsComponent
            if (HasComp<StationArrivalsComponent>(ev.Station))
            {
                // Устанавливаем SpawnResult в null, чтобы система прибытия его пропустила
                ev.SpawnResult = null;
            }
        }
    }

    /// <summary>
    ///     Проверяем, активен ли режим Battle Royale.
    /// </summary>
    private bool CheckBattleRoyaleActive()
    {
        // Ищем любой компонент BattleRoyaleRuleComponent,
        // у которого есть ActiveGameRuleComponent.
        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, ActiveGameRuleComponent>();
        return query.MoveNext(out _, out _, out _);
    }

    protected override void Started(EntityUid uid, BattleRoyaleRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Проверяем, не остался ли один игрок
        CheckLastManStanding(uid, component);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var br, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Создаем майнд для игрока
            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            // Спавним персонажа игрока на станции
            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;
            _skills.GrantAllSkills(mob);

            // Переносим mind в созданного персонажа
            _mind.TransferTo(newMind, mob);

            // Выдаем аутфит, прописанный в компоненте BattleRoyaleRuleComponent
            SetOutfitCommand.SetOutfit(mob, br.Gear, EntityManager);

            // Добавляем компонент трекинга убийств
            EnsureComp<KillTrackerComponent>(mob);

            // Помечаем событие как обработанное, чтобы предотвратить стандартный спавн
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

            // Проверяем, не остался ли один игрок в живых
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

            // Начисляем очки за убийство
            if (ev.Primary is KillPlayerSource player)
            {
                _point.AdjustPointValue(player.PlayerId, 1, uid, point);
            }

            // Начисляем ассист
            if (ev.Assist is KillPlayerSource assist)
            {
                _point.AdjustPointValue(assist.PlayerId, 0.5f, uid, point);
            }

            // Отправляем kill callout
            SendKillCallout(uid, ref ev);
        }
    }

    private void SendKillCallout(EntityUid uid, ref KillReportedEvent ev)
    {
        // Определяем, смерть ли это от окружения или суицид
        if (ev.Primary is KillEnvironmentSource || ev.Suicide)
        {
            var calloutNumber = _random.Next(0, MaxEnvironmentalCallouts + 1);
            var calloutId = $"death-match-kill-callout-env-{calloutNumber}";
            var victimName = GetEntityName(ev.Entity);

            var message = Loc.GetString(calloutId, ("victim", victimName));
            _chatManager.ChatMessageToAll(ChatChannel.Server, message, message, uid, false, true, Color.OrangeRed);
            return;
        }

        // Обычное убийство (с ассистом или без)
        string killerString;
        if (ev.Primary is KillPlayerSource primarySource)
        {
            var primaryName = GetPlayerName(primarySource.PlayerId);

            if (ev.Assist is KillPlayerSource assistSource)
            {
                // Убийство с ассистом
                var assistName = GetPlayerName(assistSource.PlayerId);
                killerString = Loc.GetString("death-match-assist", ("primary", primaryName), ("secondary", assistName));
            }
            else
            {
                // Обычное убийство
                killerString = primaryName;
            }

            var calloutNumber = _random.Next(0, MaxNormalCallouts + 1);
            var calloutId = $"death-match-kill-callout-{calloutNumber}";
            var victimName = GetEntityName(ev.Entity);

            var message = Loc.GetString(calloutId, ("killer", killerString), ("victim", victimName));
            _chatManager.ChatMessageToAll(ChatChannel.Server, message, message, uid, false, true, Color.OrangeRed);
        }
        else if (ev.Primary is KillNpcSource npcSource)
        {
            // NPC убил
            var npcName = GetEntityName(npcSource.NpcEnt);
            killerString = npcName;

            var calloutNumber = _random.Next(0, MaxNormalCallouts + 1);
            var calloutId = $"death-match-kill-callout-{calloutNumber}";
            var victimName = GetEntityName(ev.Entity);

            var message = Loc.GetString(calloutId, ("killer", killerString), ("victim", victimName));
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

        // Если остался только один игрок, он — победитель
        if (alivePlayers.Count == 1)
        {
            component.Victor = alivePlayers[0];

            if (_mind.TryGetMind(component.Victor.Value, out var mindId, out var mind))
            {
                var victorName = MetaData(component.Victor.Value).EntityName;
                var playerName = mind.Session?.Name ?? victorName;

                // Объявляем победителя
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("battle-royale-winner-announcement", ("player", playerName)));

                // Завершаем раунд с задержкой
                Timer.Spawn(component.RoundEndDelay, () =>
                {
                    if (GameTicker.RunLevel == GameRunLevel.InRound)
                        _roundEnd.EndRound();
                });
            }
        }
        // Если игроков не осталось вообще, завершаем раунд
        else if (alivePlayers.Count == 0)
        {
            component.Victor = null;
            _roundEnd.EndRound();
        }
        // Если изначально зашел только один игрок, он тоже автоматически побеждает
        else if (alivePlayers.Count == 1 && GameTicker.RunLevel == GameRunLevel.InRound &&
                 component.Victor == null && Timing.CurTime < TimeSpan.FromSeconds(10))
        {
            component.Victor = alivePlayers[0];

            if (_mind.TryGetMind(component.Victor.Value, out var mindId, out var mind))
            {
                var victorName = MetaData(component.Victor.Value).EntityName;
                var playerName = mind.Session?.Name ?? victorName;

                // Объявляем единственного игрока победителем
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("battle-royale-single-player", ("player", playerName)));

                // Завершаем раунд
                Timer.Spawn(component.RoundEndDelay, () =>
                {
                    if (GameTicker.RunLevel == GameRunLevel.InRound)
                        _roundEnd.EndRound();
                });
            }
        }
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        var query = EntityQueryEnumerator<BattleRoyaleRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var br, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Проверяем, не остался ли один игрок после отключения
            CheckLastManStanding(uid, br);
        }
    }

    private List<EntityUid> GetAlivePlayers()
    {
        var result = new List<EntityUid>();
        var mobQuery = EntityQueryEnumerator<MobStateComponent, ActorComponent>();

        while (mobQuery.MoveNext(out var uid, out var mobState, out var actor))
        {
            // Пропускаем призраков и мертвых IC
            if (HasComp<GhostBarPlayerComponent>(uid) || HasComp<IsDeadICComponent>(uid))
                continue;

            // Проверяем, что игрок подключен и его сессия активна
            if (actor.PlayerSession?.Status != SessionStatus.Connected && 
                actor.PlayerSession?.Status != SessionStatus.InGame)
                continue;

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

        // Сначала показываем победителя, если он есть
        if (component.Victor != null && _mind.TryGetMind(component.Victor.Value, out var victorMindId, out var victorMind))
        {
            var victorName = MetaData(component.Victor.Value).EntityName;
            var victorPlayerName = victorMind.Session?.Name ?? victorName;

            args.AddLine(Loc.GetString("battle-royale-winner", ("player", victorPlayerName)));
            args.AddLine("");
        }

        // Затем – таблицу убийств (scoreboard)
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
