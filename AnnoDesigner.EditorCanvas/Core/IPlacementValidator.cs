using AnnoDesigner.Controls.EditorCanvas.Content.Models;
namespace AnnoDesigner.Controls.EditorCanvas.Core;

/// <summary>
/// Validates whether an object can be placed at a given position.
/// Consumers (e.g., Anno) provide collision logic via this interface.
/// </summary>
public interface IPlacementValidator
{
    /// <summary>
    /// Returns true if the object can be placed with its current Bounds.
    /// </summary>
    bool CanPlace(ICanvasObject obj);
}
