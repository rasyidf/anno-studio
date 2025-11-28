using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Transforms;

/// <summary>
/// Resize (scale) transform operation.
/// </summary>
public class ResizeTransform : TransformOperationBase
{
    public override string Name => "Resize";
    public override string DisplayName => "Resize";
    public override string Icon => "resize_icon";

    public override bool CanExecute(IEnumerable<ICanvasObject> objects)
    {
        return objects.Any();
    }

    public override void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters)
    {
        if (!parameters.DeltaScale.HasValue)
            return;

        SavePreviousStates(objects);

        var scale = parameters.DeltaScale.Value;
        var pivot = parameters.Pivot ?? GetObjectsCenter(objects);

        foreach (var obj in objects)
        {
            var transform = obj.Transform;

            // Scale around pivot point
            if (pivot != SKPoint.Empty && pivot != transform.Position)
            {
                var offset = new SKPoint(
                    transform.Position.X - pivot.X,
                    transform.Position.Y - pivot.Y
                );

                transform.Position = new SKPoint(
                    pivot.X + offset.X * scale.X,
                    pivot.Y + offset.Y * scale.Y
                );
            }

            // Update scale
            transform.Scale = new SKPoint(
                transform.Scale.X * scale.X,
                transform.Scale.Y * scale.Y
            );

            obj.Transform = transform;
        }
    }

    private SKPoint GetObjectsCenter(IEnumerable<ICanvasObject> objects)
    {
        var objectList = objects.ToList();
        if (!objectList.Any())
            return SKPoint.Empty;

        var sumX = 0f;
        var sumY = 0f;

        foreach (var obj in objectList)
        {
            sumX += obj.Transform.Position.X;
            sumY += obj.Transform.Position.Y;
        }

        return new SKPoint(sumX / objectList.Count, sumY / objectList.Count);
    }
}
