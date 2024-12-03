using System.Linq;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Server._CorvaxNext.Storage;

/// <summary>
/// Используется для ограничения операций якорения хранилищ (не более одной сумки на тайл)
/// и выброса живых содержимых при якорении.
/// </summary>
public sealed class AnchorableStorageSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnchorableStorageComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<AnchorableStorageComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<AnchorableStorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnAnchorStateChanged(EntityUid uid, AnchorableStorageComponent comp, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        if (CheckOverlap(uid))
        {
            _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), uid);
            _xform.Unanchor(uid, xform);
            return;
        }

        // Выбрасываем любые разумные существа внутри хранилища.
        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        var entsToRemove = storage.StoredItems.Keys.Where(storedItem =>
                HasComp<MindContainerComponent>(storedItem)
            ).ToArray();

        foreach (var removeUid in entsToRemove)
            _container.RemoveEntity(uid, removeUid);
    }

    private void OnAnchorAttempt(EntityUid uid, AnchorableStorageComponent comp, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        // Если вокруг ничего нет, можем якориться без проблем.
        if (!CheckOverlap(uid))
            return;

        _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), uid, args.User);
        args.Cancel();
    }

    private void OnInsertAttempt(EntityUid uid, AnchorableStorageComponent comp, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Проверяем на наличие живых существ, они не должны вставляться при якорении.
        if (!HasComp<MindContainerComponent>(args.EntityUid))
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        if (xform.Anchored)
            args.Cancel();
    }

    [PublicAPI]
    public bool CheckOverlap(EntityUid uid)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return false;

        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        while (enumerator.MoveNext(out var otherEnt))
        {
            // Не сравниваем с самим собой.
            if (otherEnt == uid)
                continue;

            // Если другое хранилище уже закреплено здесь.
            if (HasComp<AnchorableStorageComponent>(otherEnt))
                return true;
        }

        return false;
    }
}
