using System;
using System.Linq;
using System.Windows;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    public class TransformService : ITransformService
    {
        private readonly IPreferencesService _prefs;

        private double _zoom;
        private Vector _pan;

        public event EventHandler? TransformChanged;

        public TransformService(IPreferencesService prefs)
        {
            _prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
            _zoom = prefs.DefaultZoom;
            _pan = new Vector(0, 0);

            _prefs.PropertyChanged += PrefsOnPropertyChanged;
        }

        private void PrefsOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPreferencesService.DefaultZoom) || e.PropertyName == nameof(IPreferencesService.MinZoom) || e.PropertyName == nameof(IPreferencesService.MaxZoom))
            {
                _zoom = Math.Max(_prefs.MinZoom, Math.Min(_prefs.MaxZoom, _zoom));
                TransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double Zoom
        {
            get => _zoom;
            set
            {
                var clamped = Math.Max(_prefs.MinZoom, Math.Min(_prefs.MaxZoom, value));
                if (Math.Abs(clamped - _zoom) < 1e-9) return;
                _zoom = clamped;
                TransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Vector Pan
        {
            get => _pan;
            set
            {
                if (_pan == value) return;
                _pan = value;
                TransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Point ScreenToWorld(Point screenPoint)
        {
            return new Point((screenPoint.X - _pan.X) / _zoom, (screenPoint.Y - _pan.Y) / _zoom);
        }

        public Point WorldToScreen(Point worldPoint)
        {
            return new Point(worldPoint.X * _zoom + _pan.X, worldPoint.Y * _zoom + _pan.Y);
        }

        public void ZoomAt(Point screenAnchor, double scaleFactor)
        {
            var before = ScreenToWorld(screenAnchor);
            Zoom *= scaleFactor;
            var after = ScreenToWorld(screenAnchor);
            var dx = (after.X - before.X) * Zoom;
            var dy = (after.Y - before.Y) * Zoom;
            Pan = new Vector(Pan.X + dx, Pan.Y + dy);
        }

        public void PanBy(Vector delta)
        {
            Pan = new Vector(Pan.X + delta.X, Pan.Y + delta.Y);
        }

        public Point SnapToGrid(Point worldPoint)
        {
            if (!_prefs.SnapToGrid) return worldPoint;

            var spacing = (_prefs.SubGridVisible && _prefs.SubGridSpacing > 0) ? Math.Min(_prefs.SubGridSpacing, _prefs.GridSpacing) : _prefs.GridSpacing;
            if (spacing <= 0) return worldPoint;

            var snappedX = Math.Round(worldPoint.X / spacing) * spacing;
            var snappedY = Math.Round(worldPoint.Y / spacing) * spacing;
            return new Point(snappedX, snappedY);
        }

        public Point SnapToGuideline(Point worldPoint, double[] verticalGuidelines, double[] horizontalGuidelines)
        {
            if (!_prefs.SnapToGuidelines) return worldPoint;

            var tolScreen = _prefs.SnapTolerance;
            var tolWorld = tolScreen / Math.Max(1e-6, Zoom);

            var nearestX = verticalGuidelines?.OrderBy(x => Math.Abs(x - worldPoint.X)).FirstOrDefault() ?? double.NaN;
            var nearestY = horizontalGuidelines?.OrderBy(y => Math.Abs(y - worldPoint.Y)).FirstOrDefault() ?? double.NaN;

            var snapped = worldPoint;

            if (verticalGuidelines != null && verticalGuidelines.Length > 0 && !double.IsNaN(nearestX))
            {
                if (Math.Abs(nearestX - worldPoint.X) <= tolWorld)
                {
                    snapped.X = nearestX;
                }
            }

            if (horizontalGuidelines != null && horizontalGuidelines.Length > 0 && !double.IsNaN(nearestY))
            {
                if (Math.Abs(nearestY - worldPoint.Y) <= tolWorld)
                {
                    snapped.Y = nearestY;
                }
            }

            return snapped;
        }

        public void Reset()
        {
            _zoom = _prefs.DefaultZoom;
            _pan = new Vector(0, 0);
            TransformChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
