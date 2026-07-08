using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Core;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Layers
{
    /// <summary>
    /// Renders influence circles for objects that have InfluenceRange > 0.
    /// </summary>
    public class InfluenceRenderLayer : RenderLayerBase
    {
        private static readonly Pen InfluencePen;

        private readonly Func<IEnumerable<LayoutObject>> _getObjects;

        static InfluenceRenderLayer()
        {
            InfluencePen = new Pen(Brushes.Blue, 2);
            InfluencePen.Freeze();
        }

        public InfluenceRenderLayer(Func<IEnumerable<LayoutObject>> getObjects, int order = 600)
            : base("Influence", order)
        {
            _getObjects = getObjects ?? throw new ArgumentNullException(nameof(getObjects));
        }

        public override void Render(DrawingContext dc, EditorCanvas.EditorCanvas canvas, Rect clip)
        {
            var objects = _getObjects();
            if (objects == null) return;

            foreach (var obj in objects)
            {
                var range = obj.WrappedAnnoObject.InfluenceRange;
                if (range <= 0) continue;

                // ponytail: uses GridInfluenceRangeRect center as circle center; radius is approximate.
                // Upgrade path: compute exact screen-space radius via ICoordinateHelper.
                var rangeRect = obj.GridInfluenceRangeRect;
                var center = new Point(
                    rangeRect.X + rangeRect.Width / 2,
                    rangeRect.Y + rangeRect.Height / 2);

                var radius = Math.Max(rangeRect.Width, rangeRect.Height) / 2;

                dc.DrawEllipse(null, InfluencePen, center, radius, radius);
            }
        }
    }
}
