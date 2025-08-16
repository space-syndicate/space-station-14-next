using Content.Shared._CorvaxNext.DisposablePlacer;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxNext.UserInterface.Systems.DisposablePlacer;

public sealed class DisposablePlacerBoundUserInterface : BoundUserInterface
{
    private DisposablePlacerMenu? _disposablePlacerMenu;

    public DisposablePlacerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }
    protected override void Open()
    {
        base.Open();

        _disposablePlacerMenu = this.CreateWindow<DisposablePlacerMenu>();
        _disposablePlacerMenu.SetEntity(Owner);

        _disposablePlacerMenu.DisposablePlacerSelectMessageAction += DisposablePlacerSelectMessage;
    }

    private void DisposablePlacerSelectMessage(EntProtoId protoId)
    {
        SendMessage(new DisposablePlacerSelectMessage(protoId));
    }
}
