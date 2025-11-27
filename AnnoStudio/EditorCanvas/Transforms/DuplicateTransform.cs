using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Transforms;

/// <summary>
/// Duplicate (clone) transform operation.
/// </summary>
public class DuplicateTransform : TransformOperationBase
{
    private List<ICanvasObject> _clonedObjects = new();

    public override string Name => "Duplicate";
    public override string DisplayName => "Duplicate";
    public override string Icon => "duplicate_icon";

    public override bool CanExecute(IEnumerable<ICanvasObject> objects)
    {
        return objects.Any();
    }

    public override void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters)
    {
        _clonedObjects.Clear();

        var offset = parameters.DeltaPosition ?? new SKPoint(10, 10); // Default offset

        foreach (var obj in objects)
        {
            var clone = obj.Clone();
            
            // Apply offset
            var transform = clone.Transform;
            transform.Position = new SKPoint(
                transform.Position.X + offset.X,
                transform.Position.Y + offset.Y
            );
            clone.Transform = transform;

            _clonedObjects.Add(clone);

            // Add to context if available - would need ICanvasContext passed through parameters
            // For now, caller must add to object collection
        }
    }

    public IReadOnlyList<ICanvasObject> GetClonedObjects() => _clonedObjects.AsReadOnly();
}
