using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Transforms;

/// <summary>
/// Rotate transform operation.
/// </summary>
public class RotateTransform : TransformOperationBase
{
    public override string Name => "Rotate";
    public override string DisplayName => "Rotate";
    public override string Icon => "rotate_icon";

    public override bool CanExecute(IEnumerable<ICanvasObject> objects)
    {
        return objects.Any();
    }

    public override void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters)
    {
        if (!parameters.DeltaRotation.HasValue)
            return;

        SavePreviousStates(objects);

        var angle = parameters.DeltaRotation.Value;
        var pivot = parameters.Pivot ?? GetObjectsCenter(objects);

        foreach (var obj in objects)
        {
            var transform = obj.Transform;

            // Rotate around pivot point
            if (pivot != SKPoint.Empty && pivot != transform.Position)
            {
                var translated = new SKPoint(
                    transform.Position.X - pivot.X,
                    transform.Position.Y - pivot.Y
                );

                var angleRad = angle * (float)Math.PI / 180f;
                var cos = (float)Math.Cos(angleRad);
                var sin = (float)Math.Sin(angleRad);

                var rotatedX = translated.X * cos - translated.Y * sin;
                var rotatedY = translated.X * sin + translated.Y * cos;

                transform.Position = new SKPoint(
                    rotatedX + pivot.X,
                    rotatedY + pivot.Y
                );
            }

            // Update rotation
            transform.Rotation += angle;
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
