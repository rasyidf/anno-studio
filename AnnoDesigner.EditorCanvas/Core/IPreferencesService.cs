using System;
using System.ComponentModel;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    public interface IPreferencesService : INotifyPropertyChanged
    {
        double DefaultZoom { get; set; }
        double MinZoom { get; set; }
        double MaxZoom { get; set; }

        bool GridVisible { get; set; }
        double GridSpacing { get; set; }
        bool SubGridVisible { get; set; }
        double SubGridSpacing { get; set; }
        string GridStyle { get; set; }

        bool SnapToGrid { get; set; }
        bool SnapToGuidelines { get; set; }
        double SnapTolerance { get; set; }

        event EventHandler PreferencesReset;

        void ResetToDefaults();
    }
}
