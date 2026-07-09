using System.Linq;
using AnnoDesigner.Controls.EditorCanvas.Content;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Core;

/// <summary>
/// Validates placement by checking for bounding-box collisions with existing objects.
/// </summary>
public sealed class DefaultPlacementValidator : IPlacementValidator
{
    private readonly IObjectManager<CanvasObject>? _objectManager;

    public DefaultPlacementValidator() { }

    public DefaultPlacementValidator(IObjectManager<CanvasObject> objectManager)
    {
        _objectManager = objectManager;
    }

    public bool CanPlace(ICanvasObject obj)
    {
        if (_objectManager == null || obj == null) return true;

        // Check if any existing object's bounds intersects, excluding itself (for move operations)
        return !_objectManager.GetObjectsInRegion(obj.Bounds)
            .Any(existing => existing.Id != obj.Id);
    }
}
