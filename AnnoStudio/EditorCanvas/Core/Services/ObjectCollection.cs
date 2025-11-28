using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Observable collection of canvas objects with spatial queries.
/// </summary>
public class ObjectCollection : IObjectCollection
{
    private readonly List<ICanvasObject> _objects = new();

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _objects.Count;

    public void Add(ICanvasObject obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        _objects.Add(obj);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add, obj));
    }

    public bool Remove(ICanvasObject obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var index = _objects.IndexOf(obj);
        if (index >= 0)
        {
            _objects.RemoveAt(index);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, obj, index));
            return true;
        }

        return false;
    }

    public void Clear()
    {
        if (_objects.Count > 0)
        {
            var oldItems = _objects.ToList();
            _objects.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }
    }

    public bool Contains(ICanvasObject obj)
    {
        return _objects.Contains(obj);
    }

    public ICanvasObject? GetById(Guid id)
    {
        return _objects.FirstOrDefault(o => o.Id == id);
    }

    public IEnumerable<ICanvasObject> GetObjectsAt(SKPoint point)
    {
        // Return in reverse order (top to bottom)
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            var obj = _objects[i];
            if (obj.IsVisible && !obj.IsLocked && obj.HitTest(point))
            {
                yield return obj;
            }
        }
    }

    public IEnumerable<ICanvasObject> GetObjectsInRect(SKRect rect)
    {
        foreach (var obj in _objects)
        {
            if (obj.IsVisible && rect.IntersectsWith(obj.Bounds))
            {
                yield return obj;
            }
        }
    }

    public IEnumerator<ICanvasObject> GetEnumerator()
    {
        return _objects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }
}
