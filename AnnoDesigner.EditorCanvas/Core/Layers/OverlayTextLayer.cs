using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class OverlayTextLayer : RenderLayerBase
    {
        public OverlayTextLayer(int order = 500) : base("OverlayText", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objCount = canvas.ObjectManager?.GetAll()?.Count() ?? 0;
            var activeTool = canvas.ToolManager?.ActiveTool?.Name ?? "None";
            var text = new FormattedText(
                $"Objects: {objCount} | Tool: {activeTool} | Esc: Cancel",
                System.Globalization.CultureInfo.CurrentUICulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12,
                canvas.OverlayTextBrush ?? Brushes.Black,
                1.0);

            dc.DrawText(text, new System.Windows.Point(4, 4));
        }
    }
}
