using System.Numerics;
using Content.Server.Salvage;
using Content.Shared._CorvaxNext.DynamicRange;
using Content.Shared.Salvage;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CorvaxNext.DynamicRange;

public sealed class DynamicRangeSystem : SharedDynamicRangeSystem
{
    [Dependency] private readonly RestrictedRangeSystem _restrictedRange = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    
    // Query to check if map is initialized
    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    // Keep track of previous values to detect changes
    private Dictionary<EntityUid, float> _previousRangeValues = new();
    private Dictionary<EntityUid, Vector2> _previousOriginValues = new();
    private Dictionary<EntityUid, bool> _previousShrinkValues = new();
    private Dictionary<EntityUid, float> _previousShrinkTimeValues = new();
    private Dictionary<EntityUid, float> _previousMinRangeValues = new();

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DynamicRangeComponent, ComponentStartup>(OnDynamicRangeStartup);
        SubscribeLocalEvent<DynamicRangeComponent, ComponentShutdown>(OnDynamicRangeShutdown);
        
        // Still keep this for network sync
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
                Logger.Debug($"Initialized Origin for {ToPrettyString(uid)}: {comp.Origin}");
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
                
                // If we're shrinking, recalculate the start time to maintain the current progress
                if (comp.IsShrinking && comp.ShrinkStartTime.HasValue && comp.InitialRange.HasValue)
                {
                    var elapsed = (curTime - comp.ShrinkStartTime.Value).TotalSeconds;
                    var oldProgress = elapsed / prevShrinkTime;
                    
                    // Calculate what time would have been needed to achieve current progress with new ShrinkTime
                    var newElapsed = oldProgress * comp.ShrinkTime;
                    comp.ShrinkStartTime = curTime - TimeSpan.FromSeconds(newElapsed);
                    
                    Logger.Debug($"ShrinkTime changed for {ToPrettyString(uid)}: {prevShrinkTime} -> {comp.ShrinkTime}, adjusted shrink start time");
                }
            }
            
            // Check for MinimumRange changes
            if (!_previousMinRangeValues.TryGetValue(uid, out var prevMinRange) || 
                !MathHelper.CloseTo(prevMinRange, comp.MinimumRange))
            {
                configChanged = true;
                _previousMinRangeValues[uid] = comp.MinimumRange;
                Logger.Debug($"MinimumRange changed for {ToPrettyString(uid)}: {prevMinRange} -> {comp.MinimumRange}");
            }
            
            // Check for shrinking state changes
            if (!_previousShrinkValues.TryGetValue(uid, out var prevShrinking) || prevShrinking != comp.IsShrinking)
            {
                configChanged = true;
                _previousShrinkValues[uid] = comp.IsShrinking;
                
                // If we just started shrinking, record the start time and initial range
                if (comp.IsShrinking && (!comp.ShrinkStartTime.HasValue || !comp.InitialRange.HasValue))
                {
                    comp.ShrinkStartTime = curTime;
                    comp.InitialRange = comp.Range;
                    Logger.Debug($"Started shrinking range for {ToPrettyString(uid)} from {comp.Range}");
                }
                else if (!comp.IsShrinking)
                {
                    Logger.Debug($"Stopped shrinking for {ToPrettyString(uid)} at {comp.Range}");
                }
            }
            
            // Handle shrinking logic
            if (comp.IsShrinking && comp.ShrinkStartTime.HasValue && comp.InitialRange.HasValue)
            {
                var elapsed = (curTime - comp.ShrinkStartTime.Value).TotalSeconds;
                var shrinkProgress = (float)Math.Min(elapsed / comp.ShrinkTime, 1.0);
                
                // Calculate new range
                var targetRange = Math.Max(
                    comp.MinimumRange, 
                    comp.InitialRange.Value - (comp.InitialRange.Value - comp.MinimumRange) * shrinkProgress
                );
                
                // Only update if the range has changed significantly (by at least 0.001)
                if (Math.Abs(targetRange - comp.Range) >= 0.001f)
                {
                    comp.Range = targetRange;
                    UpdateRestrictedRange(uid, comp);
                    _previousRangeValues[uid] = targetRange;
                    
                    // Debug log every whole number change for less spam
                    if (Math.Floor(targetRange * 10) != Math.Floor(_previousRangeValues[uid] * 10))
                    {
                        Logger.Debug($"Shrinking range for {ToPrettyString(uid)}: {targetRange:F1}");
                    }
                }
                
                // If we've reached minimum range, stop shrinking
                if (shrinkProgress >= 1.0f)
                {
                    comp.Range = comp.MinimumRange;
                    comp.IsShrinking = false;
                    _previousShrinkValues[uid] = false;
                    Logger.Debug($"Shrinking complete for {ToPrettyString(uid)}: Final range {comp.Range}");
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
            
            // If values changed manually, update the boundary
            if (!MathHelper.CloseTo(prevRange, comp.Range) || prevOrigin != comp.Origin)
            {
                Logger.Debug($"DynamicRange changed for {ToPrettyString(uid)}: Range {prevRange} -> {comp.Range}, Origin {prevOrigin} -> {comp.Origin}");
                UpdateRestrictedRange(uid, comp);
                
                // If range was changed manually while shrinking, update the initial range
                if (comp.IsShrinking)
                {
                    comp.InitialRange = comp.Range;
                    comp.ShrinkStartTime = curTime;
                }
                
                // Update stored values
                _previousRangeValues[uid] = comp.Range;
                _previousOriginValues[uid] = comp.Origin;
            }
        }
    }

    private void OnDynamicRangeShutdown(EntityUid uid, DynamicRangeComponent component, ComponentShutdown args)
    {
        // When component is removed, also remove RestrictedRangeComponent
        if (HasComp<RestrictedRangeComponent>(uid))
            RemComp<RestrictedRangeComponent>(uid);
            
        // Clean up tracking
        _previousRangeValues.Remove(uid);
        _previousOriginValues.Remove(uid);
        _previousShrinkValues.Remove(uid);
        _previousShrinkTimeValues.Remove(uid);
        _previousMinRangeValues.Remove(uid);
    }

    private void OnDynamicRangeStartup(EntityUid uid, DynamicRangeComponent component, ComponentStartup args)
    {
        // Mark as unprocessed so Update can handle it
        component.Processed = false;
    }

    private void OnDynamicRangeChanged(EntityUid uid, DynamicRangeComponent component, AfterAutoHandleStateEvent args)
    {
        // When component changes through network, update RestrictedRangeComponent
        UpdateRestrictedRange(uid, component);
    }

    /// <summary>
    /// Public method to directly set range and update boundary
    /// </summary>
    public void SetRange(EntityUid uid, float range, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
            
        component.Range = range;
        UpdateRestrictedRange(uid, component);
        
        // If we're shrinking, reset the shrink timer
        if (component.IsShrinking)
        {
            component.InitialRange = range;
            component.ShrinkStartTime = _timing.CurTime;
        }
        
        // Update tracking
        _previousRangeValues[uid] = range;
    }

    /// <summary>
    /// Public method to directly set origin and update boundary
    /// </summary>
    public void SetOrigin(EntityUid uid, Vector2 origin, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
            
        component.Origin = origin;
        component.OriginInitialized = true;
        UpdateRestrictedRange(uid, component);
        
        // Update tracking
        _previousOriginValues[uid] = origin;
    }
    
    /// <summary>
    /// Public method to start or stop the shrinking process
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
            Logger.Debug($"Started shrinking for {ToPrettyString(uid)} from {component.Range}");
        }
        else
        {
            // Stop shrinking
            Logger.Debug($"Stopped shrinking for {ToPrettyString(uid)} at {component.Range}");
        }
        
        _previousShrinkValues[uid] = shrinking;
    }
    
    /// <summary>
    /// Public method to set the time it takes to shrink to minimum radius
    /// </summary>
    public void SetShrinkTime(EntityUid uid, float seconds, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
            
        // Save previous value to adjust properly
        var prevShrinkTime = component.ShrinkTime;
        component.ShrinkTime = Math.Max(1f, seconds); // Minimum 1 second to avoid division by zero
        
        // If we're already shrinking, adjust the start time to maintain the current progress
        if (component.IsShrinking && component.ShrinkStartTime.HasValue && component.InitialRange.HasValue)
        {
            var elapsed = (_timing.CurTime - component.ShrinkStartTime.Value).TotalSeconds;
            var oldProgress = elapsed / prevShrinkTime;
            
            // Calculate what time would have been needed to achieve current progress with new ShrinkTime
            var newElapsed = oldProgress * component.ShrinkTime;
            component.ShrinkStartTime = _timing.CurTime - TimeSpan.FromSeconds(newElapsed);
        }
        
        _previousShrinkTimeValues[uid] = component.ShrinkTime;
        Logger.Debug($"Set ShrinkTime for {ToPrettyString(uid)} to {component.ShrinkTime}s");
    }
    
    /// <summary>
    /// Public method to set the minimum radius
    /// </summary>
    public void SetMinimumRange(EntityUid uid, float minRange, DynamicRangeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
            
        component.MinimumRange = Math.Max(1f, minRange); // Minimum 1 unit radius
        _previousMinRangeValues[uid] = component.MinimumRange;
        Logger.Debug($"Set MinimumRange for {ToPrettyString(uid)} to {component.MinimumRange}");
    }

    public void UpdateRestrictedRange(EntityUid uid, DynamicRangeComponent component)
    {
        // Check if map is initialized
        var mapInitialized = false;
        var xform = _xformQuery.GetComponent(uid);
        var mapId = xform.MapID;
        
        // Get map entity and check if it's initialized
        if (_mapManager.MapExists(mapId))
        {
            var mapUid = _mapManager.GetMapEntityId(mapId);
            mapInitialized = _mapQuery.TryComp(mapUid, out var mapComp) && mapComp.MapInitialized;
        }
        
        // If map is not initialized, delay processing
        if (!mapInitialized)
        {
            component.Processed = false;
            return;
        }
        
        // Remove old RestrictedRangeComponent and its BoundaryEntity
        if (TryComp<RestrictedRangeComponent>(uid, out var oldRestricted) && 
            oldRestricted.BoundaryEntity != EntityUid.Invalid && 
            !Deleted(oldRestricted.BoundaryEntity))
        {
            QueueDel(oldRestricted.BoundaryEntity);
        }
        
        // Create new RestrictedRangeComponent
        var restricted = EnsureComp<RestrictedRangeComponent>(uid);
        restricted.Range = component.Range;
        restricted.Origin = component.Origin;
        
        // Create new BoundaryEntity
        restricted.BoundaryEntity = _restrictedRange.CreateBoundary(
            new EntityCoordinates(uid, component.Origin), 
            component.Range);
            
        // Make sure the component is marked as dirty for network sync
        Dirty(uid, restricted);
    }
}
