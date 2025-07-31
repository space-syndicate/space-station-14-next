using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Utility;

namespace Content.Client._CorvaxNext.SurveillanceCamera.UI;

public sealed partial class SurveillanceCameraMonitorNavMapControl : NavMapControl
{
    [Dependency] private readonly IClyde _clyde = null!;
    private readonly SharedTransformSystem _transformSystem = null!;

    public IEye? Eye
    {
        get => _viewport!.Eye;
        set => _viewport!.Eye = value;
    }

    public bool ShowEye = false;

    public float ScaleModifier = 1f;

    private readonly IClydeViewport? _viewport;

    public SurveillanceCameraMonitorNavMapControl() : base()
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();
        DebugTools.AssertNotNull(_clyde);
        DebugTools.AssertNotNull(_transformSystem);

        PostWallDrawingAction += DrawCamerasViewport;
        _viewport = _clyde.CreateViewport(
            new(500, 500),
            new TextureSampleParameters
            {
                Filter = true
            });
        _viewport.RenderScale = new(1, 1);
    }

    private void DrawCamerasViewport(DrawingHandleScreen handle)
    {
        if (ShowEye && _viewport != null && Eye != null)
        {
            var offset = Offset;
            if (_physics != null)
                offset += _physics.LocalCenter;

            _viewport.Render();
            var texture = _viewport.RenderTarget.Texture;

            var camPosition = Eye.Position;
            var position = Vector2.Transform(camPosition.Position, _transformSystem.GetInvWorldMatrix(_xform!)) - offset;
            position = ScalePosition(position with { Y = -position.Y });

            var positionOffset = new Vector2(texture.Width, texture.Height) * (MinmapScaleModifier * float.Sqrt(MinimapScale) * ScaleModifier);

            var positionTopLeft = position - positionOffset;
            var positionBottomRight = position + positionOffset;

            handle.DrawRect(new UIBox2(positionTopLeft - new Vector2(1,1), positionBottomRight + new Vector2(1,1)), Color.DarkGreen, true);
            handle.DrawTextureRect(texture, new UIBox2(positionTopLeft, positionBottomRight));
        }
    }
}
