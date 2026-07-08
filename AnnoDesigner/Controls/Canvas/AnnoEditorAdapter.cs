using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using AnnoDesigner.Controls.Canvas.Layers;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas;

/// <summary>
/// Bridges the Anno-specific LayoutObject model to the generic EditorCanvas.
/// This adapter wraps an EditorCanvas instance and provides Anno-specific
/// functionality: custom render layers, placement validation, undo integration.
/// </summary>
public class AnnoEditorAdapter
{
    private readonly Dictionary<LayoutObject, LayoutObjectWrapper> _wrapperMap = new();

    public EditorCanvas.EditorCanvas Canvas { get; }

    public AnnoEditorAdapter(EditorCanvas.EditorCanvas canvas)
    {
        Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        RegisterLayers();
    }

    private void RegisterLayers()
    {
        var renderer = Canvas.LayeredRenderer;
        if (renderer == null) return;

        renderer.AddLayer(new IconRenderLayer(GetLayoutObjects, new Dictionary<string, BitmapImage>()));
        renderer.AddLayer(new BlockedAreaRenderLayer(GetLayoutObjects));
        renderer.AddLayer(new InfluenceRenderLayer(GetLayoutObjects));
    }

    private IEnumerable<LayoutObject> GetLayoutObjects() => _wrapperMap.Keys;

    public void AddLayoutObject(LayoutObject obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (_wrapperMap.ContainsKey(obj)) return;

        var wrapper = new LayoutObjectWrapper(obj);
        _wrapperMap[obj] = wrapper;
        Canvas.ObjectManager.Add(wrapper);
    }

    public void RemoveLayoutObject(LayoutObject obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (!_wrapperMap.TryGetValue(obj, out var wrapper)) return;

        _wrapperMap.Remove(obj);
        Canvas.ObjectManager.Remove(wrapper);
    }

    public int ObjectCount => _wrapperMap.Count;

    public IReadOnlyList<LayoutObject> GetAllLayoutObjects() => _wrapperMap.Keys.ToList();

    /// <summary>
    /// Loads LayoutObjects from a list (e.g., from LayoutLoader) into the EditorCanvas.
    /// Clears existing content first.
    /// </summary>
    public void LoadLayout(IEnumerable<LayoutObject> objects)
    {
        ClearAll();
        foreach (var obj in objects)
            AddLayoutObject(obj);
        Canvas.InvalidateVisual();
    }

    /// <summary>
    /// Returns all current LayoutObjects for serialization/saving.
    /// Syncs bounds back from canvas before returning.
    /// </summary>
    public IReadOnlyList<LayoutObject> GetLayoutForSave()
    {
        // Sync all wrapper bounds back to LayoutObjects
        foreach (var (layout, wrapper) in _wrapperMap)
        {
            // wrapper sync happens automatically via bidirectional binding
        }
        return _wrapperMap.Keys.ToList();
    }

    public void ClearAll()
    {
        _wrapperMap.Clear();
        Canvas.ObjectManager.Clear();
    }

    /// <summary>
    /// Syncs all wrappers from their underlying LayoutObject source (pull).
    /// Call after Anno code modifies LayoutObjects directly.
    /// </summary>
    public void SyncAllFromSource()
    {
        foreach (var wrapper in _wrapperMap.Values)
            wrapper.SyncFromSource();
    }

    /// <summary>
    /// Wraps a LayoutObject as a CanvasObject for the EditorCanvas object manager.
    /// Bidirectional: Bounds writes push back to LayoutObject.Position/Size;
    /// SyncFromSource() pulls from LayoutObject into Bounds.
    /// </summary>
    internal sealed class LayoutObjectWrapper : CanvasObject
    {
        private readonly LayoutObject _source;
        private Rect _bounds;
        private string _identifier;

        public LayoutObjectWrapper(LayoutObject source)
        {
            _source = source;
            _bounds = new Rect(source.Position, source.Size);
            _identifier = source.Identifier;
            ZIndex = 0;
            Tag = source;
            IsSelectable = true;
            ShapeType = "Rectangle";
        }

        public LayoutObject Source => _source;

        public override Rect Bounds
        {
            get => _bounds;
            set
            {
                _bounds = value;
                // ponytail: sync back to source on every Bounds write
                _source.Position = value.TopLeft;
                _source.Size = value.Size;
            }
        }

        public override string Identifier
        {
            get => _identifier;
            set
            {
                _identifier = value;
                _source.Identifier = value;
            }
        }

        /// <summary>
        /// Pull Position/Size/Identifier from the underlying LayoutObject into this wrapper.
        /// </summary>
        public void SyncFromSource()
        {
            _bounds = new Rect(_source.Position, _source.Size);
            _identifier = _source.Identifier;
        }
    }
}
