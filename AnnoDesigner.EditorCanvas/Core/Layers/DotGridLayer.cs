using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    /// <summary>
    /// A grid drawn as small dots at intersections (good for unobtrusive alignment).
    /// </summary>
    public class DotGridLayer : RenderLayerBase
    {
        public int CellSize { get; set; } = 64;
        public Brush DotBrush { get; set; } = Brushes.LightGray;
        public double DotRadius { get; set; } = 1.0;

        public DotGridLayer(int order = 120) : base("DotGrid", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            if (!canvas.ShowGrid) return;

            var brush = DotBrush ?? Brushes.LightGray;

            for (double x = 0; x <= clip.Width; x += CellSize)
            {
                for (double y = 0; y <= clip.Height; y += CellSize)
                {
                    dc.DrawEllipse(brush, null, new System.Windows.Point(x, y), DotRadius, DotRadius);
                }
            }
        }
    }
}
