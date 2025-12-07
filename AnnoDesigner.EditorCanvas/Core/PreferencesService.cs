using System;
using System.ComponentModel;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    public class PreferencesService : IPreferencesService
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? PreferencesReset;

        private double _defaultZoom = 1.0;
        private double _minZoom = 0.1;
        private double _maxZoom = 10.0;

        private bool _gridVisible = true;
        private double _gridSpacing = 64.0;
        private bool _subGridVisible = true;
        private double _subGridSpacing = 16.0;
        private string _gridStyle = "Lines";

        private bool _snapToGrid = true;
        private bool _snapToGuidelines = true;
        private double _snapTolerance = 8.0;

        public double DefaultZoom { get => _defaultZoom; set => Set(ref _defaultZoom, value); }
        public double MinZoom { get => _minZoom; set => Set(ref _minZoom, value); }
        public double MaxZoom { get => _maxZoom; set => Set(ref _maxZoom, value); }

        public bool GridVisible { get => _gridVisible; set => Set(ref _gridVisible, value); }
        public double GridSpacing { get => _gridSpacing; set => Set(ref _gridSpacing, value); }
        public bool SubGridVisible { get => _subGridVisible; set => Set(ref _subGridVisible, value); }
        public double SubGridSpacing { get => _subGridSpacing; set => Set(ref _subGridSpacing, value); }
        public string GridStyle { get => _gridStyle; set => Set(ref _gridStyle, value); }

        public bool SnapToGrid { get => _snapToGrid; set => Set(ref _snapToGrid, value); }
        public bool SnapToGuidelines { get => _snapToGuidelines; set => Set(ref _snapToGuidelines, value); }
        public double SnapTolerance { get => _snapTolerance; set => Set(ref _snapTolerance, value); }

        public void ResetToDefaults()
        {
            _defaultZoom = 1.0;
            _minZoom = 0.1;
            _maxZoom = 10.0;
            _gridVisible = true;
            _gridSpacing = 64.0;
            _subGridVisible = true;
            _subGridSpacing = 16.0;
            _gridStyle = "Lines";
            _snapToGrid = true;
            _snapToGuidelines = true;
            _snapTolerance = 8.0;

            PreferencesReset?.Invoke(this, EventArgs.Empty);
            RaisePropertyChanged(string.Empty);
        }

        private void Set<T>(ref T field, T value, string? propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            RaisePropertyChanged(propertyName);
        }

        private void RaisePropertyChanged(string? propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));
        }
    }
}
