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

            var zoom = canvas.TransformService?.Zoom ?? 1.0;

            // Selection outlines (shape-aware)
            var selected = canvas.SelectedObjects;
            if (selected != null && selected.Count > 0)
            {
                var selPen = new Pen(canvas.SelectionStrokeBrush ?? Brushes.Red, 1.5 / zoom)
                {
                    DashStyle = DashStyles.Dash
                };
                selPen.Freeze();
                foreach (var item in selected)
                {
                    if (item == null) continue;
                    switch (item.ShapeType)
                    {
                        case "Line":
                            var start = item.LineStart ?? item.Bounds.TopLeft;
                            var end = item.LineEnd ?? item.Bounds.BottomRight;
                            dc.DrawLine(selPen, start, end);
                            break;
                        case "Path":
                            if (item.PathPoints is { Count: >= 2 })
                            {
                                var geo = new StreamGeometry();
                                using (var ctx = geo.Open())
                                {
                                    ctx.BeginFigure(item.PathPoints[0], false, false);
                                    for (int i = 1; i < item.PathPoints.Count; i++)
                                        ctx.LineTo(item.PathPoints[i], true, false);
                                }
                                geo.Freeze();
                                dc.DrawGeometry(null, selPen, geo);
                            }
                            break;
                        default:
                            dc.DrawRectangle(Brushes.Transparent, selPen, item.Bounds);
                            break;
                    }
                }
            }

            // Allow active tool to draw overlays
            canvas.ToolManager?.ActiveTool?.Render(dc);
        }
    }
}
