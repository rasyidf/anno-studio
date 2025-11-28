using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Objects;

namespace AnnoStudio.EditorCanvas.Tools;

/// <summary>
/// Free-hand drawing tool - places 1x1 building objects along path.
/// </summary>
public class DrawTool : EditorToolBase
{
    private HashSet<SKPoint> _placedPositions = new();
    private SKColor _buildingColor = new SKColor(100, 180, 100);
    private string _buildingType = "Path";

    public override string Name => "Draw";
    public override string Icon => "draw_icon";
    public override string Description => "Free-hand building placement";
    public override KeyGesture? Shortcut => new KeyGesture(Key.P);
    public override ToolCursor Cursor => ToolCursor.Pen;

    public SKColor BuildingColor
    {
        get => _buildingColor;
        set => _buildingColor = value;
    }

    public string BuildingType
    {
        get => _buildingType;
        set => _buildingType = value;
    }

    public override void OnPointerPressed(PointerPressedEventArgs e, ICanvasContext context)
    {
        _placedPositions.Clear();
        PlaceBuildingAtPosition(e, context);
        e.Handled = true;
    }

    public override void OnPointerMoved(PointerEventArgs e, ICanvasContext context)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            return;

        PlaceBuildingAtPosition(e, context);
        e.Handled = true;
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e, ICanvasContext context)
    {
        _placedPositions.Clear();
        e.Handled = true;
    }

    private void PlaceBuildingAtPosition(PointerEventArgs e, ICanvasContext context)
    {
        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        var canvasPoint = context.ScreenToCanvas(position);

        // Snap to grid only if enabled
        var gridPos = context.Grid.IsEnabled ? context.Grid.SnapToGrid(canvasPoint) : canvasPoint;

        // Check if already placed at this grid position
        if (_placedPositions.Contains(gridPos))
            return;

        // Check for overlap with existing objects
        if (HasOverlap(gridPos, 1, 1, context))
            return;

        // Place building
        var building = new BuildingObject
        {
            Name = $"{_buildingType} Building",
            Width = 1,
            Height = 1,
            BuildingType = _buildingType,
            Color = _buildingColor,
            Transform = new Core.Models.Transform2D
            {
                Position = gridPos,
                Rotation = 0,
                Scale = new SKPoint(1, 1)
            }
        };

        context.Objects.Add(building);
        _placedPositions.Add(gridPos);
        context.Invalidate();
    }

    private bool HasOverlap(SKPoint position, int width, int height, ICanvasContext context)
    {
        var gridSize = context.Grid.Settings.GridSize;
        var checkBounds = new SKRect(
            position.X,
            position.Y,
            position.X + width * gridSize,
            position.Y + height * gridSize
        );

        return context.Objects.Any(obj => obj.Bounds.IntersectsWith(checkBounds));
    }

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        // No preview rendering needed for draw tool
    }
}
