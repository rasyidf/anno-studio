using System.Windows;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal interface ITransformService
    {
        Point ScreenToCanvas(Point screenPoint);
        Point CanvasToScreen(Point canvasPoint);
        double Zoom { get; set; }
    }
}
