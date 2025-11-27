using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Implementation of selection management service.
/// </summary>
public class SelectionService : ISelectionService
{
    private readonly List<ICanvasObject> _selectedObjects = new();

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public IReadOnlyList<ICanvasObject> SelectedObjects => _selectedObjects.AsReadOnly();

    public int Count => _selectedObjects.Count;

    public bool IsSelected(ICanvasObject obj)
    {
        return _selectedObjects.Contains(obj);
    }

    public void Select(ICanvasObject obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var removed = _selectedObjects.ToList();
        _selectedObjects.Clear();
        _selectedObjects.Add(obj);

        RaiseSelectionChanged(new[] { obj }, removed);
    }

    public void AddToSelection(ICanvasObject obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (!_selectedObjects.Contains(obj))
        {
            _selectedObjects.Add(obj);
            RaiseSelectionChanged(new[] { obj }, Array.Empty<ICanvasObject>());
        }
    }

    public void RemoveFromSelection(ICanvasObject obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (_selectedObjects.Remove(obj))
        {
            RaiseSelectionChanged(Array.Empty<ICanvasObject>(), new[] { obj });
        }
    }

    public void ToggleSelection(ICanvasObject obj)
    {
        if (IsSelected(obj))
        {
            RemoveFromSelection(obj);
        }
        else
        {
            AddToSelection(obj);
        }
    }

    public void SelectMultiple(IEnumerable<ICanvasObject> objects)
    {
        var objectsList = objects?.ToList() ?? new List<ICanvasObject>();
        var removed = _selectedObjects.ToList();

        _selectedObjects.Clear();
        _selectedObjects.AddRange(objectsList);

        RaiseSelectionChanged(objectsList, removed);
    }

    public void Clear()
    {
        if (_selectedObjects.Count > 0)
        {
            var removed = _selectedObjects.ToList();
            _selectedObjects.Clear();
            RaiseSelectionChanged(Array.Empty<ICanvasObject>(), removed);
        }
    }

    public void SelectAll(IEnumerable<ICanvasObject> allObjects)
    {
        var objectsList = allObjects?.ToList() ?? new List<ICanvasObject>();
        var removed = _selectedObjects.Except(objectsList).ToList();
        var added = objectsList.Except(_selectedObjects).ToList();

        _selectedObjects.Clear();
        _selectedObjects.AddRange(objectsList);

        RaiseSelectionChanged(added, removed);
    }

    public SKRect GetSelectionBounds()
    {
        if (_selectedObjects.Count == 0)
        {
            return SKRect.Empty;
        }

        var bounds = _selectedObjects[0].Bounds;
        foreach (var obj in _selectedObjects.Skip(1))
        {
            bounds.Union(obj.Bounds);
        }

        return bounds;
    }

    private void RaiseSelectionChanged(IEnumerable<ICanvasObject> added, IEnumerable<ICanvasObject> removed)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs
        {
            Selection = SelectedObjects,
            AddedObjects = added.ToList().AsReadOnly(),
            RemovedObjects = removed.ToList().AsReadOnly()
        });
    }
}
