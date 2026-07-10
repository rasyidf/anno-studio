using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class ObjectsLayer : RenderLayerBase
    {
        private static readonly Typeface LabelTypeface = new("Segoe UI");

        public ObjectsLayer(int order = 300) : base("Objects", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            // Pen thickness must be in screen pixels: 1/zoom gives 1px regardless of zoom level
            var zoom = canvas.TransformService?.Zoom ?? 1.0;
            var strokeWidth = 1.0 / zoom;

            var defaultPen = new Pen(canvas.ObjectStrokeBrush ?? Brushes.Black, strokeWidth);
            var defaultFill = canvas.ObjectFillBrush ?? Brushes.Transparent;
            defaultPen.Freeze();

            var all = canvas.ObjectManager?.GetAll();
            if (all == null) return;

            foreach (var obj in all.OrderBy(o => o.ZIndex))
            {
                if (obj == null) continue;

                // Determine fill: use object-specific FillColor, otherwise default
                Brush fill = defaultFill;
                if (obj.FillColor.HasValue)
                {
                    fill = new SolidColorBrush(obj.FillColor.Value);
                    fill.Freeze();
                }

                // Determine pen: borderless objects get no stroke
                var pen = obj.IsBorderless ? null : defaultPen;

                switch (obj.ShapeType)
                {
                    case "Line":
                        var start = obj.LineStart ?? obj.Bounds.TopLeft;
                        var end = obj.LineEnd ?? obj.Bounds.BottomRight;
                        dc.DrawLine(pen ?? defaultPen, start, end);
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
                            dc.DrawGeometry(null, pen ?? defaultPen, geo);
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
                            dc.DrawGeometry(null, pen ?? defaultPen, pathGeo);
                        }
                        break;

                    default: // "Rectangle"
                        dc.DrawRectangle(fill, pen, obj.Bounds);

                        // Draw label text when available
                        if (!string.IsNullOrEmpty(obj.Label) && obj.Bounds.Width > 0 && obj.Bounds.Height > 0)
                        {
                            var fontSize = System.Math.Max(0.3, System.Math.Min(0.8, obj.Bounds.Height * 0.2));
                            var text = new FormattedText(
                                obj.Label,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                LabelTypeface,
                                fontSize,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(canvas).PixelsPerDip)
                            {
                                MaxTextWidth = obj.Bounds.Width,
                                MaxTextHeight = obj.Bounds.Height,
                                TextAlignment = TextAlignment.Center
                            };

                            var textOrigin = new Point(
                                obj.Bounds.X,
                                obj.Bounds.Y + (obj.Bounds.Height - text.Height) / 2);

                            dc.DrawText(text, textOrigin);
                        }
                        break;
                }
            }

            // Draw selection highlighting: thin yellow dashed border (screen-pixel width)
            var selected = canvas.SelectedObjects;
            if (selected != null && selected.Count > 0)
            {
                var selPen = new Pen(Brushes.Yellow, 2.0 / zoom);
                selPen.DashStyle = DashStyles.Dash;
                selPen.Freeze();

                var selFill = new SolidColorBrush(Color.FromArgb(30, 255, 255, 0));
                selFill.Freeze();

                foreach (var obj in selected)
                {
                    if (obj == null) continue;
                    dc.DrawRectangle(selFill, selPen, obj.Bounds);
                }
            }
        }
    }
}
