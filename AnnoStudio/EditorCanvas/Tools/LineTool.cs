using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Objects;

namespace AnnoStudio.EditorCanvas.Tools;

/// <summary>
/// Places buildings in a straight line.
/// </summary>
public class LineTool : EditorToolBase
{
    private SKPoint? _startPoint;
    private SKPoint? _endPoint;
    private SKColor _buildingColor = new SKColor(180, 100, 100);
    private string _buildingType = "Wall";
    private List<SKPoint> _previewPositions = new();

    public override string Name => "Line";
    public override string Icon => "line_icon";
    public override string Description => "Place buildings in a line";
    public override KeyGesture? Shortcut => new KeyGesture(Key.L);
    public override ToolCursor Cursor => ToolCursor.Cross;

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
        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        _startPoint = context.ScreenToCanvas(position);
        
        // Snap to grid if enabled
        if (context.Grid.IsEnabled)
        {
            _startPoint = context.Grid.SnapToGrid(_startPoint.Value);
        }

        _endPoint = _startPoint;

        e.Handled = true;
    }

    public override void OnPointerMoved(PointerEventArgs e, ICanvasContext context)
    {
        if (!_startPoint.HasValue)
            return;
        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        _endPoint = context.ScreenToCanvas(position);

        // Snap to grid only if enabled
        if (context.Grid.IsEnabled)
        {
            _endPoint = context.Grid.SnapToGrid(_endPoint.Value);
        }

        // Calculate preview positions
        _previewPositions = CalculateLinePositions(_startPoint.Value, _endPoint.Value, context);

        context.Invalidate();
        e.Handled = true;
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e, ICanvasContext context)
    {
        if (_startPoint.HasValue && _endPoint.HasValue)
        {
            var positions = CalculateLinePositions(_startPoint.Value, _endPoint.Value, context);

            foreach (var pos in positions)
            {
                if (!HasOverlap(pos, 1, 1, context))
                {
                    var building = new BuildingObject
                    {
                        Name = $"{_buildingType} Building",
                        Width = 1,
                        Height = 1,
                        BuildingType = _buildingType,
                        Color = _buildingColor,
                        Transform = new Core.Models.Transform2D
                        {
                            Position = pos,
                            Rotation = 0,
                            Scale = new SKPoint(1, 1)
                        }
                    };
                    context.Objects.Add(building);
                }
            }
        }

        _startPoint = null;
        _endPoint = null;
        _previewPositions.Clear();

        e.Handled = true;
    }

    private List<SKPoint> CalculateLinePositions(SKPoint start, SKPoint end, ICanvasContext context)
    {
        var positions = new List<SKPoint>();
        var gridSize = context.Grid.Settings.GridSize;

        // Convert to grid coordinates and use Bresenham's algorithm for consistent cell iteration
        var (x0, y0) = context.Grid.CanvasToGrid(start);
        var (x1, y1) = context.Grid.CanvasToGrid(end);

        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy; // error value e_xy

        int x = x0;
        int y = y0;

        while (true)
        {
            var canvasPos = context.Grid.GridToCanvas(x, y);
            if (!positions.Contains(canvasPos))
                positions.Add(canvasPos);

            if (x == x1 && y == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
        }

        return positions;
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
        if (!_previewPositions.Any())
            return;

        var gridSize = context.Grid.Settings.GridSize;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = _buildingColor.WithAlpha(128),
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White.WithAlpha(180),
            StrokeWidth = 2,
            IsAntialias = true
        };

        foreach (var pos in _previewPositions)
        {
            var rect = new SKRect(pos.X, pos.Y, pos.X + gridSize, pos.Y + gridSize);
            canvas.DrawRect(rect, paint);
            canvas.DrawRect(rect, strokePaint);
        }
    }
}
