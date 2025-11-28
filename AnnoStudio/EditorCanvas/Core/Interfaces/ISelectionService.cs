using System;
using System.Collections.Generic;
using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Manages object selection on the canvas.
/// </summary>
public interface ISelectionService
{
    /// <summary>
    /// Currently selected objects.
    /// </summary>
    IReadOnlyList<ICanvasObject> SelectedObjects { get; }

    /// <summary>
    /// Number of selected objects.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Check if object is selected.
    /// </summary>
    bool IsSelected(ICanvasObject obj);

    /// <summary>
    /// Select single object (clears previous selection).
    /// </summary>
    void Select(ICanvasObject obj);

    /// <summary>
    /// Add object to selection.
    /// </summary>
    void AddToSelection(ICanvasObject obj);

    /// <summary>
    /// Remove object from selection.
    /// </summary>
    void RemoveFromSelection(ICanvasObject obj);

    /// <summary>
    /// Toggle object selection state.
    /// </summary>
    void ToggleSelection(ICanvasObject obj);

    /// <summary>
    /// Select multiple objects (clears previous selection).
    /// </summary>
    void SelectMultiple(IEnumerable<ICanvasObject> objects);

    /// <summary>
    /// Clear all selections.
    /// </summary>
    void Clear();

    /// <summary>
    /// Select all objects.
    /// </summary>
    void SelectAll(IEnumerable<ICanvasObject> allObjects);

    /// <summary>
    /// Get bounding box of all selected objects.
    /// </summary>
    SKRect GetSelectionBounds();

    /// <summary>
    /// Event raised when selection changes.
    /// </summary>
    event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
}

/// <summary>
/// Event arguments for selection change events.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public IReadOnlyList<ICanvasObject> Selection { get; init; } = Array.Empty<ICanvasObject>();
    public IReadOnlyList<ICanvasObject> AddedObjects { get; init; } = Array.Empty<ICanvasObject>();
    public IReadOnlyList<ICanvasObject> RemovedObjects { get; init; } = Array.Empty<ICanvasObject>();
}
