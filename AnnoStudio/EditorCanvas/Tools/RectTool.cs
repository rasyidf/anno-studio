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
/// Places buildings in a rectangular area.
/// </summary>
public class RectTool : EditorToolBase
{
    private SKPoint? _startPoint;
    private SKPoint? _endPoint;
    private SKColor _buildingColor = new SKColor(100, 100, 180);
    private string _buildingType = "Residence";
    private bool _fillArea = true;
    private List<SKPoint> _previewPositions = new();

    public override string Name => "Rectangle";
    public override string Icon => "rect_icon";
    public override string Description => "Place buildings in a rectangle";
    public override KeyGesture? Shortcut => new KeyGesture(Key.R);
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

    public bool FillArea
    {
        get => _fillArea;
        set => _fillArea = value;
    }

    public override void OnPointerPressed(PointerPressedEventArgs e, ICanvasContext context)
    {
        // Use position relative to the canvas control to avoid offset issues
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
        // Use position relative to the canvas control
        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        _endPoint = context.ScreenToCanvas(position);

        // Snap to grid only if enabled
        if (context.Grid.IsEnabled)
        {
            _endPoint = context.Grid.SnapToGrid(_endPoint.Value);
        }

        // Calculate preview positions
        _previewPositions = CalculateRectPositions(_startPoint.Value, _endPoint.Value, context);

        context.Invalidate();
        e.Handled = true;
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e, ICanvasContext context)
    {
        if (_startPoint.HasValue && _endPoint.HasValue)
        {
            var positions = CalculateRectPositions(_startPoint.Value, _endPoint.Value, context);

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

    private List<SKPoint> CalculateRectPositions(SKPoint start, SKPoint end, ICanvasContext context)
    {
        var positions = new List<SKPoint>();
        // Convert canvas positions to grid coordinates to avoid rounding inconsistencies
        var (startX, startY) = context.Grid.CanvasToGrid(start);
        var (endX, endY) = context.Grid.CanvasToGrid(end);

        var gridLeft = Math.Min(startX, endX);
        var gridTop = Math.Min(startY, endY);
        var gridRight = Math.Max(startX, endX);
        var gridBottom = Math.Max(startY, endY);

        if (_fillArea)
        {
            // Fill entire area
            for (int x = gridLeft; x <= gridRight; x++)
            {
                for (int y = gridTop; y <= gridBottom; y++)
                {
                    positions.Add(context.Grid.GridToCanvas(x, y));
                }
            }
        }
        else
        {
            // Only outline
            for (int x = gridLeft; x <= gridRight; x++)
            {
                positions.Add(context.Grid.GridToCanvas(x, gridTop));
                positions.Add(context.Grid.GridToCanvas(x, gridBottom));
            }
            for (int y = gridTop + 1; y < gridBottom; y++)
            {
                positions.Add(context.Grid.GridToCanvas(gridLeft, y));
                positions.Add(context.Grid.GridToCanvas(gridRight, y));
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
