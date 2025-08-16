using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared._CorvaxNext.DisposablePlacer;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._CorvaxNext.UserInterface.Systems.DisposablePlacer;

public sealed partial class DisposablePlacerMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public event Action<EntProtoId>? DisposablePlacerSelectMessageAction;

    public EntityUid Entity { get; set; }

    public DisposablePlacerMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var main = FindControl<RadialContainer>("Main");

        if (!_entityManager.TryGetComponent<DisposablePlacerComponent>(Entity, out var comp))
            return;

        foreach (var selectedProtoId in comp.SelectablePrototypes)
        {
            if (!_prototypeManager.TryIndex(selectedProtoId.Id, out var prototype))
                return;

            var button = new DisposablePlacerMenuButton()
            {
                SetSize = new Vector2(64, 64),
                ToolTip = Loc.GetString(prototype.Name ?? string.Empty),
                ProtoId = selectedProtoId.Id,
            };

            var entProtoView = new EntityPrototypeView()
            {
                SetSize = new Vector2(48, 48),
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Stretch = SpriteView.StretchMode.Fill
            };

            entProtoView.SetPrototype(selectedProtoId);

            button.AddChild(entProtoView);

            button.OnPressed += _ =>
            {
                DisposablePlacerSelectMessageAction?.Invoke(selectedProtoId);

                if (_playerManager.LocalSession?.AttachedEntity == null)
                    return;

                var popup = _entityManager.System<PopupSystem>();
                popup.PopupClient(Loc.GetString("disposable-placer-selected-popup", ("mode", Loc.GetString(prototype.Name ?? string.Empty))), Entity, _playerManager.LocalSession.AttachedEntity);
                Close();
            };

            main.AddChild(button);
        }
    }
}

public sealed class DisposablePlacerMenuButton : RadialMenuTextureButtonWithSector
{
    public EntProtoId ProtoId { get; set; }
}
