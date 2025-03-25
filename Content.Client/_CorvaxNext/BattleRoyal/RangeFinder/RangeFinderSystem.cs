using Content.Shared._CorvaxNext.RangeFinder;
using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._CorvaxNext.RangeFinder;

public sealed class RangeFinderSystem : SharedRangeFinderSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Обновляем направление стрелки в соответствии с поворотом камеры
        var query = EntityQueryEnumerator<RangeFinderComponent, SpriteComponent>();
        while (query.MoveNext(out var _, out var rangeFinder, out var sprite))
        {
            // Если компонент неактивен, не вращаем стрелку
            if (!rangeFinder.IsActive)
            {
                sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                continue;
            }

            // Если нет цели, не вращаем стрелку
            if (rangeFinder.DistanceToTarget == Distance.Unknown)
            {
                sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                continue;
            }

            var eye = _eyeManager.CurrentEye;
            var angle = rangeFinder.ArrowAngle + eye.Rotation;

            switch (rangeFinder.DistanceToTarget)
            {
                case Distance.Close:
                case Distance.Medium:
                case Distance.Far:
                case Distance.Reached:
                    sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                default:
                    sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
            }
        }
    }
}
