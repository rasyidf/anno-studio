using System;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Implementation of grid system.
/// </summary>
public class GridSystem : IGridSystem
{
    private GridSettings _settings = new();

    public GridSettings Settings
    {
        get => _settings;
        set => _settings = value ?? new GridSettings();
    }

    public bool IsEnabled { get; set; } = true;

    public SKPoint SnapToGrid(SKPoint point)
    {
        if (!IsEnabled || !_settings.SnapEnabled)
        {
            return point;
        }

        var gridSize = _settings.GridSize;
        var offset = _settings.Offset;

        var snappedX = (float)Math.Round((point.X - offset.X) / gridSize) * gridSize + offset.X;
        var snappedY = (float)Math.Round((point.Y - offset.Y) / gridSize) * gridSize + offset.Y;

        return new SKPoint(snappedX, snappedY);
    }

    public SKRect SnapToGrid(SKRect rect)
    {
        if (!IsEnabled || !_settings.SnapEnabled)
        {
            return rect;
        }

        var topLeft = SnapToGrid(new SKPoint(rect.Left, rect.Top));
        var bottomRight = SnapToGrid(new SKPoint(rect.Right, rect.Bottom));

        return new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    public GridCell GetCellAt(SKPoint point)
    {
        var (x, y) = CanvasToGrid(point);
        var topLeft = GridToCanvas(x, y);
        var bounds = new SKRect(topLeft.X, topLeft.Y, topLeft.X + _settings.GridSize, topLeft.Y + _settings.GridSize);

        return new GridCell(x, y, bounds);
    }

    public SKPoint GridToCanvas(int gridX, int gridY)
    {
        var offset = _settings.Offset;
        var gridSize = _settings.GridSize;

        return new SKPoint(
            gridX * gridSize + offset.X,
            gridY * gridSize + offset.Y
        );
    }

    public (int x, int y) CanvasToGrid(SKPoint canvasPoint)
    {
        var offset = _settings.Offset;
        var gridSize = _settings.GridSize;

        var x = (int)Math.Floor((canvasPoint.X - offset.X) / gridSize);
        var y = (int)Math.Floor((canvasPoint.Y - offset.Y) / gridSize);

        return (x, y);
    }
}
