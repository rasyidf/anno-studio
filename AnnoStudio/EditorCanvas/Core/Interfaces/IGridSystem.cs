using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Provides grid functionality for the canvas.
/// </summary>
public interface IGridSystem
{
    /// <summary>
    /// Grid settings.
    /// </summary>
    GridSettings Settings { get; set; }

    /// <summary>
    /// Whether grid is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Snap point to grid.
    /// </summary>
    SKPoint SnapToGrid(SKPoint point);

    /// <summary>
    /// Snap rectangle to grid.
    /// </summary>
    SKRect SnapToGrid(SKRect rect);

    /// <summary>
    /// Get grid cell at point.
    /// </summary>
    GridCell GetCellAt(SKPoint point);

    /// <summary>
    /// Convert grid coordinates to canvas coordinates.
    /// </summary>
    SKPoint GridToCanvas(int gridX, int gridY);

    /// <summary>
    /// Convert canvas coordinates to grid coordinates.
    /// </summary>
    (int x, int y) CanvasToGrid(SKPoint canvasPoint);
}

/// <summary>
/// Represents a grid cell.
/// </summary>
public record GridCell(int X, int Y, SKRect Bounds);
