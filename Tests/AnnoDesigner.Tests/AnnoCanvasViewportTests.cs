using System.Reflection;
using System.Windows;
using Xunit;

namespace AnnoDesigner.Tests
{
    public class AnnoCanvasViewportTests
    {
        [StaFact]
        public void CenterViewportOnRect_CentersSelectionInViewport()
        {
            var canvas = new AnnoDesigner.Controls.Canvas.AnnoCanvas();

            // set viewport size and scrollable bounds so behavior is deterministic
            var viewportField = typeof(AnnoDesigner.Controls.Canvas.AnnoCanvas).GetField("_viewport", BindingFlags.NonPublic | BindingFlags.Instance);
            var scrollBoundsField = typeof(AnnoDesigner.Controls.Canvas.AnnoCanvas).GetField("_scrollableBounds", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(viewportField);
            Assert.NotNull(scrollBoundsField);

            var viewport = (AnnoDesigner.Viewport)viewportField.GetValue(canvas)!;
            viewport.Width = 50;
            viewport.Height = 20;
            viewport.Left = 0;
            viewport.Top = 0;

            // large scrollable area
            scrollBoundsField.SetValue(canvas, new Rect(0, 0, 1000, 1000));

            // center on a small rect
            var selection = new Rect(100, 100, 20, 20);

            canvas.CenterViewportOnRect(selection);

            // expected left = 100 + (20 - 50)/2 = 85
            Assert.Equal(85, canvas.ViewportLeft, 3);
            // expected top = 100 + (20 - 20)/2 = 100
            Assert.Equal(100, canvas.ViewportTop, 3);
        }
    }
}
