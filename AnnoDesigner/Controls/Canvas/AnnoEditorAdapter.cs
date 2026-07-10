using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using AnnoDesigner.Controls.Canvas.Layers;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;
using AnnoDesigner.Core.Layout.Helper;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Controls.Canvas;

/// <summary>
/// Bridges the Anno-specific LayoutObject model to the generic EditorCanvas.
/// This adapter wraps an EditorCanvas instance and provides Anno-specific
/// functionality: custom render layers, placement validation, undo integration.
/// </summary>
public class AnnoEditorAdapter
{
    private readonly Dictionary<LayoutObject, LayoutObjectWrapper> _wrapperMap = new();
    private readonly ICoordinateHelper _coordinateHelper;
    private readonly IBrushCache _brushCache;
    private readonly IPenCache _penCache;
    private readonly Func<int> _getGridSize;
    private readonly Dictionary<string, IconImage> _icons;
    private InfluenceRenderLayer _influenceLayer;

    public EditorCanvas.EditorCanvas Canvas { get; }

    public AnnoEditorAdapter(
        EditorCanvas.EditorCanvas canvas,
        ICoordinateHelper coordinateHelper = null,
        IBrushCache brushCache = null,
        IPenCache penCache = null,
        Func<int> getGridSize = null,
        Dictionary<string, IconImage> icons = null)
    {
        Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _coordinateHelper = coordinateHelper ?? new CoordinateHelper();
        _brushCache = brushCache ?? new BrushCache();
        _penCache = penCache ?? new PenCache();
        _getGridSize = getGridSize ?? (() => AnnoDesigner.Constants.GridStepDefault);
        _icons = icons ?? new Dictionary<string, IconImage>();
        RegisterLayers();
    }

    private Dictionary<string, BitmapImage> BuildIconLookup()
    {
        var lookup = new Dictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, iconImage) in _icons)
        {
            if (string.IsNullOrEmpty(iconImage?.IconPath)) continue;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(iconImage.IconPath, UriKind.RelativeOrAbsolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                // Store by icon name (without path/extension) for lookup
                var iconName = System.IO.Path.GetFileNameWithoutExtension(iconImage.IconPath);
                if (!string.IsNullOrEmpty(iconName) && !lookup.ContainsKey(iconName))
                    lookup[iconName] = bmp;
                // Also store by the key (which may be the icon name already)
                if (!lookup.ContainsKey(key))
                    lookup[key] = bmp;
            }
            catch
            {
                // ponytail: skip icons that fail to load (missing files, bad paths)
            }
        }
        return lookup;
    }

    private void RegisterLayers()
    {
        var renderer = Canvas.LayeredRenderer;
        if (renderer == null) return;

        renderer.AddLayer(new IconRenderLayer(GetLayoutObjects, _getGridSize, BuildIconLookup()));
        renderer.AddLayer(new BlockedAreaRenderLayer(GetLayoutObjects));

        _influenceLayer = new InfluenceRenderLayer(
            GetLayoutObjects,
            order: 250,
            getInfluencePolygonPoints: GetInfluencePolygonPoints);
        renderer.AddLayer(_influenceLayer);
    }

    /// <summary>
    /// Computes road-network influence polygon points for a given object.
    /// Only computes for objects with PavedStreet set (road-connected influence).
    /// Falls back to null (circle rendering) when road data isn't available.
    /// </summary>
    private IEnumerable<Point> GetInfluencePolygonPoints(LayoutObject obj)
    {
        var annoObj = obj.WrappedAnnoObject;
        if (annoObj == null || annoObj.InfluenceRange <= 0)
            return null;

        // ponytail: only compute expensive polygon for PavedStreet objects.
        // Ceiling: extend to all objects with road-connectivity when perf allows.
        if (!annoObj.PavedStreet)
            return null;

        var allAnnoObjects = _wrapperMap.Keys.Select(lo => lo.WrappedAnnoObject).ToList();
        if (allAnnoObjects.Count == 0)
            return null;

        var gridDictionary = RoadSearchHelper.PrepareGridDictionary(allAnnoObjects);
        if (gridDictionary == null)
            return null;

        var startObjects = new[] { annoObj };
        var visitedCells = RoadSearchHelper.BreadthFirstSearch(
            allAnnoObjects,
            startObjects,
            o => (int)o.InfluenceRange,
            gridDictionary);

        if (visitedCells == null || visitedCells.Length == 0)
            return null;

        var boundaryPoints = PolygonBoundaryFinderHelper.GetBoundaryPoints(visitedCells);
        if (boundaryPoints.Count == 0)
            return null;

        // Convert grid-relative boundary points back to absolute grid coordinates
        var result = new List<Point>(boundaryPoints.Count);
        foreach (var (x, y) in boundaryPoints)
        {
            result.Add(new Point(x + gridDictionary.Offset.x, y + gridDictionary.Offset.y));
        }

        return result;
    }

    private IEnumerable<LayoutObject> GetLayoutObjects() => _wrapperMap.Keys;

    public void AddLayoutObject(LayoutObject obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (_wrapperMap.ContainsKey(obj)) return;

        var wrapper = new LayoutObjectWrapper(obj);
        _wrapperMap[obj] = wrapper;
        Canvas.ObjectManager.Add(wrapper);
        _influenceLayer?.InvalidatePolygonCache();
    }

    public void RemoveLayoutObject(LayoutObject obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (!_wrapperMap.TryGetValue(obj, out var wrapper)) return;

        _wrapperMap.Remove(obj);
        Canvas.ObjectManager.Remove(wrapper);
        _influenceLayer?.InvalidatePolygonCache();
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

    /// <summary>
    /// Loads raw AnnoObjects (e.g., from LayoutLoader.LoadLayout) into the EditorCanvas.
    /// Constructs LayoutObject wrappers internally.
    /// </summary>
    public void LoadLayout(IEnumerable<AnnoObject> annoObjects)
    {
        ClearAll();
        foreach (var annoObj in annoObjects)
        {
            var layoutObj = new LayoutObject(annoObj, _coordinateHelper, _brushCache, _penCache);
            AddLayoutObject(layoutObj);
        }
        Canvas.InvalidateVisual();
    }

    /// <summary>
    /// Loads a LayoutFile (from LayoutLoader) into the EditorCanvas.
    /// </summary>
    public void LoadLayoutFile(LayoutFile layoutFile)
    {
        if (layoutFile?.Objects == null) return;
        LoadLayout(layoutFile.Objects);
    }

    /// <summary>
    /// Returns all AnnoObjects for saving via LayoutLoader.SaveLayout.
    /// </summary>
    public IEnumerable<AnnoObject> GetAnnoObjectsForSave()
    {
        return _wrapperMap.Keys.Select(lo => lo.WrappedAnnoObject);
    }

    public void ClearAll()
    {
        _wrapperMap.Clear();
        Canvas.ObjectManager.Clear();
        _influenceLayer?.InvalidatePolygonCache();
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
            SyncFromSource();
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
        /// Pull all properties from the underlying LayoutObject into this wrapper.
        /// </summary>
        public void SyncFromSource()
        {
            _bounds = new Rect(_source.Position, _source.Size);
            _identifier = _source.Identifier;
            FillColor = _source.Color.MediaColor;
            IconName = _source.WrappedAnnoObject?.Icon;
            Label = _source.WrappedAnnoObject?.Label;
            IsRoad = _source.WrappedAnnoObject?.Road ?? false;
            IsBorderless = _source.WrappedAnnoObject?.Borderless ?? false;
            ZIndex = IsRoad ? -1 : 0; // roads render below buildings
        }
    }
}
