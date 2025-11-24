using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Controls.Canvas.Services;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using Xunit;

namespace AnnoDesigner.Tests
{
    public class CanvasRendererTests
    {
        [StaFact]
        public void DrawHoverHighlight_DoesNotThrow()
        {
            // Arrange
            var renderer = new CanvasRenderer(null);
            var layoutObject = new LayoutObject(new AnnoObject { Position = new Point(2, 3), Size = new Size(2, 2) }, new Helper.CoordinateHelper(), null, null);

            var visual = new DrawingVisual();

            // Act / Assert - simply ensure the method executes without throwing
            using (var dc = visual.RenderOpen())
            {
                renderer.DrawHoverHighlight(dc, layoutObject, new Pen(Brushes.Black, 1), gridSize: 32);
            }
        }

        [StaFact]
        public void DrawGrid_DoesNotThrow()
        {
            var renderer = new CanvasRenderer(null);
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                renderer.DrawGrid(dc, width: 100, height: 60, horizontalAlignmentValue: 0, verticalAlignmentValue: 0, gridSize: 16, forceRedraw: true, gridLinePen: new Pen(Brushes.Black, 1), guidelineSet: new GuidelineSet());
            }
        }
    }
}
