using Avalonia.Input;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Base;

/// <summary>
/// Base class for editor tools with common functionality.
/// </summary>
public abstract class EditorToolBase : IEditorTool
{
    public abstract string Name { get; }
    public abstract string Icon { get; }
    public virtual string Description => string.Empty;
    public virtual ToolCursor Cursor => ToolCursor.Default;
    public virtual KeyGesture? Shortcut => null;
    public virtual bool IsEnabled => true;

    protected SKPoint? CurrentPoint { get; set; }
    protected bool IsActive { get; private set; }

    public virtual void OnPointerPressed(PointerPressedEventArgs args, ICanvasContext context)
    {
    }

    public virtual void OnPointerMoved(PointerEventArgs args, ICanvasContext context)
    {
    }

    public virtual void OnPointerReleased(PointerReleasedEventArgs args, ICanvasContext context)
    {
    }

    public virtual void OnActivated(ICanvasContext context)
    {
        IsActive = true;
    }

    public virtual void OnDeactivated(ICanvasContext context)
    {
        IsActive = false;
        CurrentPoint = null;
    }

    public virtual void Render(SKCanvas canvas, ICanvasContext context)
    {
    }

    public virtual bool OnKeyDown(KeyEventArgs args)
    {
        return false;
    }

    public virtual bool OnKeyUp(KeyEventArgs args)
    {
        return false;
    }

    protected SKPoint GetCanvasPoint(PointerEventArgs args, ICanvasContext context)
    {
        var position = args.GetPosition(args.Source as Avalonia.Visual);
        return context.ScreenToCanvas(position);
    }
}
