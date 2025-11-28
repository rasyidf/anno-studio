using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Observable collection of canvas objects.
/// </summary>
public interface IObjectCollection : IEnumerable<ICanvasObject>, INotifyCollectionChanged
{
    /// <summary>
    /// Number of objects in collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Add an object to the collection.
    /// </summary>
    void Add(ICanvasObject obj);

    /// <summary>
    /// Remove an object from the collection.
    /// </summary>
    bool Remove(ICanvasObject obj);

    /// <summary>
    /// Clear all objects.
    /// </summary>
    void Clear();

    /// <summary>
    /// Check if object exists in collection.
    /// </summary>
    bool Contains(ICanvasObject obj);

    /// <summary>
    /// Get object by ID.
    /// </summary>
    ICanvasObject? GetById(Guid id);

    /// <summary>
    /// Get objects at specific point.
    /// </summary>
    IEnumerable<ICanvasObject> GetObjectsAt(SkiaSharp.SKPoint point);

    /// <summary>
    /// Get objects within rectangle.
    /// </summary>
    IEnumerable<ICanvasObject> GetObjectsInRect(SkiaSharp.SKRect rect);
}
