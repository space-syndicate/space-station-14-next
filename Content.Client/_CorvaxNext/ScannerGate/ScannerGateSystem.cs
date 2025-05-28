using Content.Shared._CorvaxNext.ScannerGate;
using Robust.Client.GameObjects;

namespace Content.Client._CorvaxNext.ScannerGate
{
    public sealed class ScannerGateSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ScannerGateComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }

        private void OnAppearanceChange(EntityUid uid, ScannerGateComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!_appearance.TryGetData<ScannerGateStatusVisualState>(uid, ScannerGateVisualLayers.Status, out var state, args.Component))
                state = ScannerGateStatusVisualState.Off;

            switch (state)
            {
                case ScannerGateStatusVisualState.Idle:
                    SetState(args.Sprite, component.VisualStateIdle);
                    break;
                case ScannerGateStatusVisualState.Passed:
                    SetState(args.Sprite, component.VisualStatePassed);
                    break;
                case ScannerGateStatusVisualState.Denied:
                    SetState(args.Sprite, component.VisualStateDenied);
                    break;
                case ScannerGateStatusVisualState.Off:
                    args.Sprite.LayerSetVisible(ScannerGateVisualLayers.Status, false);
                    break;
            }
        }

        private void SetState(SpriteComponent sprite, string layer)
        {
            sprite.LayerSetVisible(ScannerGateVisualLayers.Status, true);
            sprite.LayerSetState(ScannerGateVisualLayers.Status, layer);
        }
    }
}
