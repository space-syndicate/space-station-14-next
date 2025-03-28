using System.Numerics;
using System.Linq;
using Content.Server.Salvage;
using Content.Shared._CorvaxNext.BattleRoyale.DynamicRange;
using Content.Shared.Salvage;
using Content.Server.Damage;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Audio;
using Robust.Shared.Audio;
using Content.Shared.Audio;

namespace Content.Server._CorvaxNext.BattleRoyale.DynamicRange;

public sealed class DynamicRangeSystem : SharedDynamicRangeSystem
{
    [Dependency] private readonly RestrictedRangeSystem _restrictedRange = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;

    // Query to check if the map is initialized
    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    // Keep track of previous values to detect changes
    private Dictionary<EntityUid, float> _previousRangeValues = new();
    private Dictionary<EntityUid, Vector2> _previousOriginValues = new();
    private Dictionary<EntityUid, bool> _previousShrinkValues = new();
    private Dictionary<EntityUid, float> _previousShrinkTimeValues = new();
    private Dictionary<EntityUid, float> _previousMinRangeValues = new();

    private const float DamageInterval = 1.0f; // Damage interval in seconds
    private const float OutOfBoundsDamage = 10.0f; // Damage amount per tick

    private const float SearchRangeMultiplier = 3f; // Multiplier for search radius
    private const float MinSearchRange = 100f; // Minimum search radius

    private readonly DamageSpecifier _suffocationDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Asphyxiation", FixedPoint2.New(OutOfBoundsDamage) }
        }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicRangeComponent, ComponentStartup>(OnDynamicRangeStartup);
        SubscribeLocalEvent<DynamicRangeComponent, ComponentShutdown>(OnDynamicRangeShutdown);

        // Keep for network synchronization
        SubscribeLocalEvent<DynamicRangeComponent, AfterAutoHandleStateEvent>(OnDynamicRangeChanged);

        _mapQuery = GetEntityQuery<MapComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        // Process all DynamicRangeComponents to check for changes
        var query = EntityQueryEnumerator<DynamicRangeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Initialize origin if needed
            if (!comp.OriginInitialized)
            {
                // Set random origin within bounds
                comp.Origin = new Vector2(
                    _random.NextFloat(comp.MinOriginX, comp.MaxOriginX),
                    _random.NextFloat(comp.MinOriginY, comp.MaxOriginY)
                );
                comp.OriginInitialized = true;
            }

            // First initialization
            if (!comp.Processed)
            {
                UpdateRestrictedRange(uid, comp);
                comp.Processed = true;

                // Store initial values
                _previousRangeValues[uid] = comp.Range;
                _previousOriginValues[uid] = comp.Origin;
                _previousShrinkValues[uid] = comp.IsShrinking;
                _previousShrinkTimeValues[uid] = comp.ShrinkTime;
                _previousMinRangeValues[uid] = comp.MinimumRange;
                continue;
            }

            // Check if configuration values have changed
            bool configChanged = false;

            // Check for ShrinkTime changes
            if (!_previousShrinkTimeValues.TryGetValue(uid, out var prevShrinkTime) ||
                !MathHelper.CloseTo(prevShrinkTime, comp.ShrinkTime))
            {
                configChanged = true;
                _previousShrinkTimeValues[uid] = comp.ShrinkTime;

                // If shrinking, recalculate the start time to maintain current progress
                if (comp.IsShrinking && comp.ShrinkStartTime.HasValue && comp.InitialRange.HasValue)
                {
                    var elapsed = (curTime - comp.ShrinkStartTime.Value).TotalSeconds;
                    var oldProgress = elapsed / prevShrinkTime;
                    var newElapsed = oldProgress * comp.ShrinkTime;
                    comp.ShrinkStartTime = curTime - TimeSpan.FromSeconds(newElapsed);
                }
            }

            // Check for MinimumRange changes
            if (!_previousMinRangeValues.TryGetValue(uid, out var prevMinRange) ||
                !MathHelper.CloseTo(prevMinRange, comp.MinimumRange))
            {
                configChanged = true;
                _previousMinRangeValues[uid] = comp.MinimumRange;
            }

            // Check for shrinking state changes
            if (!_previousShrinkValues.TryGetValue(uid, out var prevShrinking) || prevShrinking != comp.IsShrinking)
            {
                configChanged = true;
                _previousShrinkValues[uid] = comp.IsShrinking;

                // If just started shrinking, record the start time and initial range
                if (comp.IsShrinking && (!comp.ShrinkStartTime.HasValue || !comp.InitialRange.HasValue))
                {
                    comp.ShrinkStartTime = curTime;
                    comp.InitialRange = comp.Range;
                }
            }

            // Handle shrinking logic
            if (comp.IsShrinking && comp.ShrinkStartTime.HasValue && comp.InitialRange.HasValue)
            {
                var elapsed = (curTime - comp.ShrinkStartTime.Value).TotalSeconds;
                var shrinkProgress = (float)Math.Min(elapsed / comp.ShrinkTime, 1.0);
                var timeRemaining = comp.ShrinkTime - elapsed;

                // Calculate new range
                var targetRange = Math.Max(
                    comp.MinimumRange,
                    comp.InitialRange.Value - (comp.InitialRange.Value - comp.MinimumRange) * shrinkProgress
                );

                // Play music when approaching minimum range
                if (timeRemaining <= comp.MusicStartTime && !comp.PlayedMusic)
                {
                    var music = new SoundCollectionSpecifier("NukeMusic");
                    _sound.DispatchStationEventMusic(uid, music, StationEventMusicType.Nuke);
                    comp.PlayedMusic = true;
                }

                // Update only if the range has changed significantly (by at least 0.001)
                if (Math.Abs(targetRange - comp.Range) >= 0.001f)
                {
                    comp.Range = targetRange;
                    UpdateRestrictedRange(uid, comp);
                    _previousRangeValues[uid] = targetRange;
                }

                // Stop shrinking if minimum range is reached
                if (shrinkProgress >= 1.0f)
                {
                    comp.Range = comp.MinimumRange;
                    comp.IsShrinking = false;
                    _previousShrinkValues[uid] = false;
                }
            }

            // Check if Range or Origin values have changed manually
            if (!_previousRangeValues.TryGetValue(uid, out var prevRange) ||
                !_previousOriginValues.TryGetValue(uid, out var prevOrigin))
            {
                _previousRangeValues[uid] = comp.Range;
                _previousOriginValues[uid] = comp.Origin;
                continue;
            }

            // Update the boundary if values changed manually
            if (!MathHelper.CloseTo(prevRange, comp.Range) || prevOrigin != comp.Origin)
            {
                UpdateRestrictedRange(uid, comp);

                // If range was changed manually during shrinking, update initial range
                if (comp.IsShrinking)
                {
                    comp.InitialRange = comp.Range;
                    comp.ShrinkStartTime = curTime;
                }

                _previousRangeValues[uid] = comp.Range;
                _previousOriginValues[uid] = comp.Origin;
            }

            var searchRadius = Math.Max(MinSearchRange, comp.Range * SearchRangeMultiplier);
            var coordinates = new EntityCoordinates(uid, comp.Origin);
            var players = _lookup.GetEntitiesInRange(coordinates, searchRadius, LookupFlags.Dynamic | LookupFlags.Approximate)
                .Where(e => HasComp<MobStateComponent>(e));

            foreach (var player in players)
            {
                var playerPos = _transform.GetWorldPosition(player);
                var distance = (playerPos - comp.Origin).Length();

                // If the player is outside the safe zone
                if (distance > comp.Range)
                {
                    // Check if it's time to apply damage
                    if (!comp.LastDamageTimes.TryGetValue(player, out var lastDamage) ||
                        (curTime - lastDamage).TotalSeconds >= DamageInterval)
                    {
                        _damageableSystem.TryChangeDamage(player, _suffocationDamage, origin: uid);
                        comp.LastDamageTimes[player] = curTime;
                    }
                }
                else
                {
                    // Remove the player from the tracking dictionary if they returned to the safe zone
                    comp.LastDamageTimes.Remove(player);
                }
            }
        }
    }

    private void OnDynamicRangeShutdown(EntityUid uid, DynamicRangeComponent component, ComponentShutdown args)
    {
        // When the component is removed, also remove the RestrictedRangeComponent
        if (HasComp<RestrictedRangeComponent>(uid))
            RemComp<RestrictedRangeComponent>(uid);

        // Stop any playing music
        if (component.PlayedMusic)
        {
            _sound.StopStationEventMusic(uid, StationEventMusicType.Nuke);
        }

        // Clean up tracking dictionaries
        _previousRangeValues.Remove(uid);
        _previousOriginValues.Remove(uid);
        _previousShrinkValues.Remove(uid);
        _previousShrinkTimeValues.Remove(uid);
        _previousMinRangeValues.Remove(uid);
    }

    private void OnDynamicRangeStartup(EntityUid uid, DynamicRangeComponent component, ComponentStartup args)
    {
        // Mark as unprocessed so Update can handle initialization
        component.Processed = false;
    }

    private void OnDynamicRangeChanged(EntityUid uid, DynamicRangeComponent component, AfterAutoHandleStateEvent args)
    {
        // When the component changes via network, update the RestrictedRangeComponent
        UpdateRestrictedRange(uid, component);
    }

    /// <summary>
    /// Public method to directly set range and update the boundary.
    /// </summary>
    public void SetRange(EntityUid uid, float range, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Range = range;
        UpdateRestrictedRange(uid, component);

        // If shrinking, reset the shrink timer
        if (component.IsShrinking)
        {
            component.InitialRange = range;
            component.ShrinkStartTime = _timing.CurTime;
        }

        _previousRangeValues[uid] = range;
    }

    /// <summary>
    /// Public method to directly set origin and update the boundary.
    /// </summary>
    public void SetOrigin(EntityUid uid, Vector2 origin, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Origin = origin;
        component.OriginInitialized = true;
        UpdateRestrictedRange(uid, component);

        _previousOriginValues[uid] = origin;
    }

    /// <summary>
    /// Public method to start or stop the shrinking process.
    /// </summary>
    public void SetShrinking(EntityUid uid, bool shrinking, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsShrinking == shrinking)
            return;

        component.IsShrinking = shrinking;

        if (shrinking)
        {
            // Start shrinking
            component.ShrinkStartTime = _timing.CurTime;
            component.InitialRange = component.Range;
        }
        else if (component.PlayedMusic)
        {
            // Stop music if we're stopping the shrinking
            _sound.StopStationEventMusic(uid, StationEventMusicType.Nuke);
            component.PlayedMusic = false;
        }

        _previousShrinkValues[uid] = shrinking;
    }

    /// <summary>
    /// Public method to set the time it takes to shrink to the minimum radius.
    /// </summary>
    public void SetShrinkTime(EntityUid uid, float seconds, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var prevShrinkTime = component.ShrinkTime;
        component.ShrinkTime = Math.Max(1f, seconds); // Minimum 1 second to avoid division by zero

        // If already shrinking, adjust the start time to maintain current progress
        if (component.IsShrinking && component.ShrinkStartTime.HasValue && component.InitialRange.HasValue)
        {
            var elapsed = (_timing.CurTime - component.ShrinkStartTime.Value).TotalSeconds;
            var oldProgress = elapsed / prevShrinkTime;
            var newElapsed = oldProgress * component.ShrinkTime;
            component.ShrinkStartTime = _timing.CurTime - TimeSpan.FromSeconds(newElapsed);
        }

        _previousShrinkTimeValues[uid] = component.ShrinkTime;
    }

    /// <summary>
    /// Public method to set the minimum radius.
    /// </summary>
    public void SetMinimumRange(EntityUid uid, float minRange, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.MinimumRange = Math.Max(1f, minRange); // Minimum 1 unit radius
        _previousMinRangeValues[uid] = component.MinimumRange;
    }

    public void UpdateRestrictedRange(EntityUid uid, DynamicRangeComponent component)
    {
        // Check if the map is initialized
        var mapInitialized = false;
        var xform = _xformQuery.GetComponent(uid);
        var mapId = xform.MapID;

        // Get the map entity and check its initialization state
        if (_mapManager.MapExists(mapId))
        {
            var mapUid = _mapManager.GetMapEntityId(mapId);
            mapInitialized = _mapQuery.TryComp(mapUid, out var mapComp) && mapComp.MapInitialized;
        }

        // Delay processing if the map is not initialized
        if (!mapInitialized)
        {
            component.Processed = false;
            return;
        }

        // Remove the old RestrictedRangeComponent and its BoundaryEntity
        if (TryComp<RestrictedRangeComponent>(uid, out var oldRestricted) &&
            oldRestricted.BoundaryEntity != EntityUid.Invalid &&
            !Deleted(oldRestricted.BoundaryEntity))
        {
            QueueDel(oldRestricted.BoundaryEntity);
        }

        // Create a new RestrictedRangeComponent
        var restricted = EnsureComp<RestrictedRangeComponent>(uid);
        restricted.Range = component.Range;
        restricted.Origin = component.Origin;

        // Create a new BoundaryEntity
        restricted.BoundaryEntity = _restrictedRange.CreateBoundary(
            new EntityCoordinates(uid, component.Origin),
            component.Range);

        // Mark the component as dirty for network synchronization
        Dirty(uid, restricted);
    }
}
