using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class ToolingOverlayLayer : RenderLayerBase
    {
        public ToolingOverlayLayer(int order = 400) : base("ToolOverlays", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            if (!canvas.ShowToolOverlays) return;

            // Selection outlines
            var selected = canvas.SelectedObjects;
            if (selected != null && selected.Count > 0)
            {
                var selPen = new Pen(canvas.SelectionStrokeBrush ?? Brushes.Red, 1.5)
                {
                    DashStyle = DashStyles.Dash
                };
                selPen.Freeze();
                foreach (var item in selected)
                {
                    if (item == null) continue;
                    dc.DrawRectangle(Brushes.Transparent, selPen, item.Bounds);
                }
            }

            // Allow active tool to draw overlays
            canvas.ToolManager?.ActiveTool?.Render(dc);
        }
    }
}
