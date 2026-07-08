using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class ObjectsLayer : RenderLayerBase
    {
        public ObjectsLayer(int order = 300) : base("Objects", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var pen = new Pen(canvas.ObjectStrokeBrush ?? Brushes.Blue, 1);
            var fill = canvas.ObjectFillBrush ?? Brushes.Transparent;
            pen.Freeze();

            var all = canvas.ObjectManager?.GetAll();
            if (all == null) return;

            foreach (var obj in all.OrderBy(o => o.ZIndex))
            {
                if (obj == null) continue;

                switch (obj.ShapeType)
                {
                    case "Line":
                        var start = obj.LineStart ?? obj.Bounds.TopLeft;
                        var end = obj.LineEnd ?? obj.Bounds.BottomRight;
                        dc.DrawLine(pen, start, end);
                        break;

                    case "Path":
                        if (obj.PathPoints is { Count: >= 2 })
                        {
                            var geo = new StreamGeometry();
                            using (var ctx = geo.Open())
                            {
                                ctx.BeginFigure(obj.PathPoints[0], false, false);
                                for (int i = 1; i < obj.PathPoints.Count; i++)
                                    ctx.LineTo(obj.PathPoints[i], true, false);
                            }
                            geo.Freeze();
                            dc.DrawGeometry(null, pen, geo);
                        }
                        break;

                    case "Curve":
                        if (obj.BezierPoints is { Count: >= 2 })
                        {
                            var pathGeo = new PathGeometry();
                            var figure = new PathFigure { StartPoint = obj.BezierPoints[0].Point, IsClosed = false, IsFilled = false };
                            for (int i = 1; i < obj.BezierPoints.Count; i++)
                            {
                                var prev = obj.BezierPoints[i - 1];
                                var curr = obj.BezierPoints[i];
                                var cp1 = prev.ControlOut ?? prev.Point;
                                var cp2 = curr.ControlIn ?? curr.Point;
                                figure.Segments.Add(new BezierSegment(cp1, cp2, curr.Point, true));
                            }
                            pathGeo.Figures.Add(figure);
                            pathGeo.Freeze();
                            dc.DrawGeometry(null, pen, pathGeo);
                        }
                        break;

                    default: // "Rectangle"
                        dc.DrawRectangle(fill, pen, obj.Bounds);
                        break;
                }
            }

            // Draw selection highlighting
            var selected = canvas.SelectedObjects;
            if (selected != null && selected.Count > 0)
            {
                var selPen = new Pen(canvas.SelectionStrokeBrush ?? Brushes.Red, 2);
                selPen.Freeze();
                foreach (var obj in selected)
                {
                    if (obj == null) continue;
                    dc.DrawRectangle(null, selPen, obj.Bounds);
                }
            }
        }
    }
}
