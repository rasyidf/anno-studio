using AnnoDesigner.Controls.EditorCanvas.Content.Models;
namespace AnnoDesigner.Controls.EditorCanvas.Core;

/// <summary>
/// Default no-op validator — always allows placement.
/// </summary>
public sealed class DefaultPlacementValidator : IPlacementValidator
{
    public bool CanPlace(ICanvasObject obj) => true;
}
