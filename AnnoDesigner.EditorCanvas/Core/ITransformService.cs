using System.Windows;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    public interface ITransformService
    {
        double Zoom { get; set; }
        Vector Pan { get; set; }

        event System.EventHandler TransformChanged;

        Point ScreenToWorld(Point screenPoint);
        Point WorldToScreen(Point worldPoint);

        void ZoomAt(Point screenAnchor, double scaleFactor);
        void PanBy(Vector delta);

        Point SnapToGrid(Point worldPoint);
        Point SnapToGuideline(Point worldPoint, double[] verticalGuidelines, double[] horizontalGuidelines);

        void Reset();
    }
}
