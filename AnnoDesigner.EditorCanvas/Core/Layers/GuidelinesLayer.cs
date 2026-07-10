using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class GuidelinesLayer : RenderLayerBase
    {
        public GuidelinesLayer(int order = 200) : base("Guidelines", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            if (!canvas.ShowGuides) return;

            var zoom = canvas.TransformService?.Zoom ?? 1.0;
            var pen = new Pen(canvas.GuideLineBrush ?? Brushes.Gray, 1.0 / zoom);
            pen.DashStyle = DashStyles.Dash;
            pen.Freeze();

            double cx = clip.Width / 2.0;
            double cy = clip.Height / 2.0;
            dc.DrawLine(pen, new System.Windows.Point(cx, 0), new System.Windows.Point(cx, clip.Height));
            dc.DrawLine(pen, new System.Windows.Point(0, cy), new System.Windows.Point(clip.Width, cy));
        }
    }
}
