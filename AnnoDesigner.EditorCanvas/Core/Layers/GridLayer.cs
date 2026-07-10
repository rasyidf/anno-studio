using System;
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

            // Draw grid lines covering the visible clip area in world coordinates.
            // Snap start to nearest cell boundary below/left of clip origin.
            var startX = Math.Floor(clip.Left / CellSize) * CellSize;
            var startY = Math.Floor(clip.Top / CellSize) * CellSize;

            for (double x = startX; x <= clip.Right; x += CellSize)
            {
                dc.DrawLine(pen, new Point(x, clip.Top), new Point(x, clip.Bottom));
            }
            for (double y = startY; y <= clip.Bottom; y += CellSize)
            {
                dc.DrawLine(pen, new Point(clip.Left, y), new Point(clip.Right, y));
            }
        }
    }
}
