using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    /// <summary>
    /// A grid drawn as small crosses at the grid intersections.
    /// </summary>
    public class CrossGridLayer : RenderLayerBase
    {
        public int CellSize { get; set; } = 64;
        public Brush CrossBrush { get; set; } = Brushes.LightGray;
        public double CrossHalfSize { get; set; } = 3.0;
        public double Thickness { get; set; } = 0.75;

        public CrossGridLayer(int order = 130) : base("CrossGrid", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            if (!canvas.ShowGrid) return;

            var pen = new Pen(CrossBrush ?? Brushes.LightGray, Thickness);
            pen.Freeze();

            for (double x = 0; x <= clip.Width; x += CellSize)
            {
                for (double y = 0; y <= clip.Height; y += CellSize)
                {
                    // small cross centered at (x,y)
                    dc.DrawLine(pen, new System.Windows.Point(x - CrossHalfSize, y), new System.Windows.Point(x + CrossHalfSize, y));
                    dc.DrawLine(pen, new System.Windows.Point(x, y - CrossHalfSize), new System.Windows.Point(x, y + CrossHalfSize));
                }
            }
        }
    }
}
