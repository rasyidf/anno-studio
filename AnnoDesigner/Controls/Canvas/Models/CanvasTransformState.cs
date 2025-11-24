using System.Windows;

namespace AnnoDesigner.Controls.Canvas.Models
{
    internal class CanvasTransformState
    {
        public Point Offset { get; set; }
        public double Zoom { get; set; } = 1.0;
    }
}
