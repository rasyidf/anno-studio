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
                dc.DrawRectangle(fill, pen, obj.Bounds);
            }
        }
    }
}
