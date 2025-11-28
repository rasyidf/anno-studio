using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Transforms;

/// <summary>
/// Move (translate) transform operation.
/// </summary>
public class MoveTransform : TransformOperationBase
{
    public override string Name => "Move";
    public override string DisplayName => "Move";
    public override string Icon => "move_icon";

    public override bool CanExecute(IEnumerable<ICanvasObject> objects)
    {
        return objects.Any();
    }

    public override void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters)
    {
        if (!parameters.DeltaPosition.HasValue)
            return;

        SavePreviousStates(objects);

        var delta = parameters.DeltaPosition.Value;

        foreach (var obj in objects)
        {
            var newTransform = obj.Transform;
            newTransform.Position = new SKPoint(
                newTransform.Position.X + delta.X,
                newTransform.Position.Y + delta.Y
            );
            obj.Transform = newTransform;
        }
    }
}
