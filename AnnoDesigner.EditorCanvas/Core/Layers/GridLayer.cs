using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class GridLayer : RenderLayerBase
    {
        public int CellSize { get; set; } = 128;

        public GridLayer(int order = 100) : base("Grid", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            if (!canvas.ShowGrid) return;

            var pen = new Pen(canvas.GridLineBrush ?? Brushes.LightGray, 0.5);
            pen.Freeze();

            for (double x = 0; x <= clip.Width; x += CellSize)
            {
                dc.DrawLine(pen, new System.Windows.Point(x, 0), new System.Windows.Point(x, clip.Height));
            }
            for (double y = 0; y <= clip.Height; y += CellSize)
            {
                dc.DrawLine(pen, new System.Windows.Point(0, y), new System.Windows.Point(clip.Width, y));
            }
        }
    }
}
