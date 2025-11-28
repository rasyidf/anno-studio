using System;
using System.Collections.Concurrent;
using AnnoStudio.EditorCanvas.Core.Interfaces;

using SkiaSharp;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Event bus implementation for canvas events.
/// </summary>
public class CanvasEventBus : ICanvasEventBus
{
    public event EventHandler<SKPoint>? CursorPositionChanged;
    
    public void PublishCursorPositionChanged(SKPoint position)
    {
        CursorPositionChanged?.Invoke(this, position);
    }
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _subscribers = new();

    public void Publish<T>(T eventData) where T : ICanvasEvent
    {
        var eventType = typeof(T);

        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler is Action<T> typedHandler)
                {
                    try
                    {
                        typedHandler(eventData);
                    }
                    catch
                    {
                        // Silently ignore handler exceptions to prevent one bad handler from breaking others
                    }
                }
            }
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : ICanvasEvent
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var eventType = typeof(T);
        var handlers = _subscribers.GetOrAdd(eventType, _ => new ConcurrentBag<object>());
        handlers.Add(handler);

        return new Subscription(() =>
        {
            if (_subscribers.TryGetValue(eventType, out var bag))
            {
                var newBag = new ConcurrentBag<object>();
                foreach (var h in bag)
                {
                    if (h != (object)handler)
                    {
                        newBag.Add(h);
                    }
                }
                _subscribers[eventType] = newBag;
            }
        });
    }

    public void Clear()
    {
        _subscribers.Clear();
    }

    private class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe();
                _disposed = true;
            }
        }
    }
}
