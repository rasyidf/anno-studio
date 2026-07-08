using System.Windows;
using System.Windows.Media;

namespace AnnoDesigner.Controls.EditorCanvas.Core.Layers
{
    public class BackgroundLayer : RenderLayerBase
    {
        public BackgroundLayer(int order = 0) : base("Background", order)
        {
        }

        public override void Render(DrawingContext dc, EditorCanvas canvas, Rect clip)
        {
            var brush = canvas.BackgroundBrush ?? Brushes.White;
            dc.DrawRectangle(brush, null, clip);
        }
    }
}
