using Avalonia.Input;
using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Represents an interactive editing tool for the canvas.
/// </summary>
public interface IEditorTool
{
    /// <summary>
    /// Unique identifier for the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Icon path or resource key for UI display.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Description shown in tooltips and UI.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Cursor to display when tool is active.
    /// </summary>
    ToolCursor Cursor { get; }

    /// <summary>
    /// Keyboard shortcut for activating the tool.
    /// </summary>
    KeyGesture? Shortcut { get; }

    /// <summary>
    /// Whether the tool is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Called when pointer is pressed on canvas.
    /// </summary>
    void OnPointerPressed(PointerPressedEventArgs args, ICanvasContext context);

    /// <summary>
    /// Called when pointer moves over canvas.
    /// </summary>
    void OnPointerMoved(PointerEventArgs args, ICanvasContext context);

    /// <summary>
    /// Called when pointer is released.
    /// </summary>
    void OnPointerReleased(PointerReleasedEventArgs args, ICanvasContext context);

    /// <summary>
    /// Called when tool becomes active.
    /// </summary>
    void OnActivated(ICanvasContext context);

    /// <summary>
    /// Called when tool becomes inactive.
    /// </summary>
    void OnDeactivated(ICanvasContext context);

    /// <summary>
    /// Render tool-specific overlay on the canvas.
    /// </summary>
    void Render(SKCanvas canvas, ICanvasContext context);

    /// <summary>
    /// Called on key press while tool is active.
    /// </summary>
    bool OnKeyDown(KeyEventArgs args);

    /// <summary>
    /// Called on key release while tool is active.
    /// </summary>
    bool OnKeyUp(KeyEventArgs args);
}

/// <summary>
/// Cursor types for editor tools.
/// </summary>
public enum ToolCursor
{
    Default,
    Cross,
    Hand,
    Move,
    Pen,
    Eraser,
    Picker,
    Custom
}
