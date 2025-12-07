using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    /// <summary>
    /// A finer "sub-grid" drawn on top of the main grid to help with alignment.
    /// </summary>
    public class SubGridLayer : RenderLayerBase
    {
        public int CellSize { get; set; } = 32;
        public Brush LineBrush { get; set; } = Brushes.LightGray;

        public SubGridLayer(int order = 110) : base("SubGrid", order)
        {
        }

        public override void Render(DrawingContext dc, AnnoDesigner.Controls.EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            if (!canvas.ShowGrid) return;

            var pen = new Pen(LineBrush ?? Brushes.LightGray, 0.25);
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
