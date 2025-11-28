using System;

using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Event bus for canvas-wide events.
/// </summary>
public interface ICanvasEventBus
{
    event EventHandler<SKPoint>? CursorPositionChanged;
    void PublishCursorPositionChanged(SKPoint position);
    /// <summary>
    /// Publish an event.
    /// </summary>
    void Publish<T>(T eventData) where T : ICanvasEvent;

    /// <summary>
    /// Subscribe to events of a specific type.
    /// </summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : ICanvasEvent;

    /// <summary>
    /// Unsubscribe all handlers.
    /// </summary>
    void Clear();
}

/// <summary>
/// Marker interface for canvas events.
/// </summary>
public interface ICanvasEvent
{
    DateTime Timestamp { get; }
}
