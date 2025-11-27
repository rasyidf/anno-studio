using System;
using System.Linq;
using Avalonia.Input;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Tools;

/// <summary>
/// Selects and transforms objects.
/// </summary>
public class SelectTool : EditorToolBase
{
    private SKPoint _dragStart;
    private SKPoint _currentPos;
    private bool _isDragging;
    private bool _isBoxSelecting;

    public override string Name => "Select";
    public override string Icon => "select_icon";
    public override string Description => "Select and transform objects";
    public override KeyGesture? Shortcut => new KeyGesture(Key.V);
    public override ToolCursor Cursor => ToolCursor.Default;

    public override void OnActivated(ICanvasContext context)
    {
        base.OnActivated(context);
        _isDragging = false;
        _isBoxSelecting = false;
    }

    public override void OnPointerPressed(PointerPressedEventArgs e, ICanvasContext context)
    {
        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        var worldPos = context.ScreenToCanvas(position);

        _dragStart = worldPos;
        _currentPos = worldPos;

        // Check if clicking on selected object
        var clickedObject = context.Objects
            .Reverse()
            .FirstOrDefault(obj => obj.HitTest(worldPos));

        if (clickedObject != null)
        {
            // Shift+Click for multi-select
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (context.Selection.IsSelected(clickedObject))
                    context.Selection.RemoveFromSelection(clickedObject);
                else
                    context.Selection.AddToSelection(clickedObject);
            }
            else if (!context.Selection.IsSelected(clickedObject))
            {
                context.Selection.Clear();
                context.Selection.AddToSelection(clickedObject);
            }

            _isDragging = true;
        }
        else
        {
            // Start box selection
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                context.Selection.Clear();
            }
            
            _isBoxSelecting = true;
        }

        context.Invalidate();
        e.Handled = true;
    }

    public override void OnPointerMoved(PointerEventArgs e, ICanvasContext context)
    {
        if (!_isDragging && !_isBoxSelecting)
            return;

        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        var worldPos = context.ScreenToCanvas(position);

        if (_isDragging)
        {
            // Move selected objects
            var delta = new SKPoint(
                worldPos.X - _currentPos.X,
                worldPos.Y - _currentPos.Y
            );

            foreach (var obj in context.Selection.SelectedObjects)
            {
                obj.Transform = new Core.Models.Transform2D
                {
                    Position = new SKPoint(
                        obj.Transform.Position.X + delta.X,
                        obj.Transform.Position.Y + delta.Y
                    ),
                    Rotation = obj.Transform.Rotation,
                    Scale = obj.Transform.Scale
                };
            }
        }

        _currentPos = worldPos;
        // Request redraw so selection box updates while dragging
        context.Invalidate();
        e.Handled = true;
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e, ICanvasContext context)
    {
        if (_isBoxSelecting)
        {
            // Select all objects within box
            var left = Math.Min(_dragStart.X, _currentPos.X);
            var top = Math.Min(_dragStart.Y, _currentPos.Y);
            var right = Math.Max(_dragStart.X, _currentPos.X);
            var bottom = Math.Max(_dragStart.Y, _currentPos.Y);
            var selectionRect = new SKRect(left, top, right, bottom);

            var objectsInBox = context.Objects
                .Where(obj => selectionRect.IntersectsWith(obj.Bounds))
                .ToList();

            foreach (var obj in objectsInBox)
            {
                if (!context.Selection.IsSelected(obj))
                    context.Selection.AddToSelection(obj);
            }
        }

        _isDragging = false;
        _isBoxSelecting = false;
        // Ensure final redraw after selection ends
        context.Invalidate();
        e.Handled = true;
    }

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (!_isBoxSelecting)
            return;

        // Draw selection box in canvas space
        var left = Math.Min(_dragStart.X, _currentPos.X);
        var top = Math.Min(_dragStart.Y, _currentPos.Y);
        var right = Math.Max(_dragStart.X, _currentPos.X);
        var bottom = Math.Max(_dragStart.Y, _currentPos.Y);
        var rect = new SKRect(left, top, right, bottom);

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DodgerBlue,
            StrokeWidth = 2 / context.Viewport.Zoom, // Scale-independent line width
            PathEffect = SKPathEffect.CreateDash(new[] { 5f / context.Viewport.Zoom, 5f / context.Viewport.Zoom }, 0),
            IsAntialias = true
        };

        canvas.DrawRect(rect, paint);

        // Fill with semi-transparent blue
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColors.DodgerBlue.WithAlpha(30);
        paint.PathEffect = null;
        canvas.DrawRect(rect, paint);
    }
}
