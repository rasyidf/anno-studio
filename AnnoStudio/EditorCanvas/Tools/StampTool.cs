using Avalonia.Input;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Objects;

namespace AnnoStudio.EditorCanvas.Tools;

/// <summary>
/// Stamps building objects at click position.
/// </summary>
public class StampTool : EditorToolBase
{
    private BuildingObject? _template;
    private BuildingObject? _preview;

    public override string Name => "Stamp";
    public override string Icon => "stamp_icon";
    public override string Description => "Place building stamps on the canvas";
    public override KeyGesture? Shortcut => new KeyGesture(Key.S);
    public override ToolCursor Cursor => ToolCursor.Cross;

    /// <summary>
    /// Sets the template object to stamp.
    /// </summary>
    public void SetTemplate(BuildingObject template)
    {
        _template = template;
    }

    public override void OnActivated(ICanvasContext context)
    {
        base.OnActivated(context);
        _preview = null;
    }

    public override void OnDeactivated(ICanvasContext context)
    {
        base.OnDeactivated(context);
        _preview = null;
    }

    public override void OnPointerPressed(PointerPressedEventArgs e, ICanvasContext context)
    {
        if (_template == null)
            return;

        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        var worldPos = context.ScreenToCanvas(position);

        // Snap to grid if enabled
        if (context.Grid.IsEnabled)
        {
            worldPos = context.Grid.SnapToGrid(worldPos);
        }

        // Create a new instance from template
        var newObject = new BuildingObject
        {
            Name = _template.Name,
            Width = _template.Width,
            Height = _template.Height,
            BuildingType = _template.BuildingType,
            Color = _template.Color,
            Icon = _template.Icon,
            Transform = new Core.Models.Transform2D 
            { 
                Position = worldPos,
                Rotation = _template.Transform.Rotation,
                Scale = _template.Transform.Scale
            }
        };

        // Add to context
        context.Objects.Add(newObject);

        e.Handled = true;
    }

    public override void OnPointerMoved(PointerEventArgs e, ICanvasContext context)
    {
        if (_template == null)
            return;

        var position = context is Avalonia.Controls.Control c ? e.GetPosition(c) : e.GetPosition(null);
        var worldPos = context.ScreenToCanvas(position);

        // Snap to grid if enabled
        if (context.Grid.IsEnabled)
        {
            worldPos = context.Grid.SnapToGrid(worldPos);
        }

        // Update preview position
        if (_preview == null)
        {
            _preview = new BuildingObject
            {
                Name = _template.Name,
                Width = _template.Width,
                Height = _template.Height,
                BuildingType = _template.BuildingType,
                Color = _template.Color,
                Icon = _template.Icon,
                Transform = new Core.Models.Transform2D 
                { 
                    Position = worldPos,
                    Rotation = _template.Transform.Rotation,
                    Scale = _template.Transform.Scale
                }
            };
        }
        else
        {
            _preview.Transform = new Core.Models.Transform2D 
            { 
                Position = worldPos,
                Rotation = _preview.Transform.Rotation,
                Scale = _preview.Transform.Scale
            };
        }

        e.Handled = true;
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e, ICanvasContext context)
    {
        e.Handled = true;
    }

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (_preview == null)
            return;

        // Render preview with transparency
        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(128)
        };

        canvas.Save();
        
        var renderContext = new Core.Models.RenderContext
        {
            Viewport = context.Viewport,
            Settings = context.Settings.Render,
            GridSize = context.Grid.Settings.GridSize,
            Selection = context.Selection
        };

        _preview.Render(canvas, renderContext);
        
        canvas.Restore();
    }
}
