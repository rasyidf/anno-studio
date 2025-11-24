using System.Windows;
using AnnoDesigner.Controls.Canvas.Services;
using AnnoDesigner.Helper;
using Xunit;

namespace AnnoDesigner.Tests
{
    public class InputInteractionServiceTests
    {
        [Fact]
        public void MouseWheel_NoZoomToPoint_ChangesGridSize()
        {
            var svc = new InputInteractionService();
            var viewport = new Viewport { Left = 0, Top = 0, Width = 100, Height = 100 };
            var coord = new CoordinateHelper();

            var result = svc.HandleMouseWheel(120, new Point(0, 0), 10, false, Constants.ZoomSensitivityPercentageDefault, viewport, false, coord);

            Assert.True(result.NewGridSize > 10);
        }

        [Fact]
        public void MouseWheel_UseZoomToPoint_AdjustsViewportOffset()
        {
            var svc = new InputInteractionService();
            var viewport = new Viewport { Left = 0, Top = 0, Width = 200, Height = 200 };
            var coord = new CoordinateHelper();

            var mouse = new Point(50, 80);
            int currentGrid = 10;

            var result = svc.HandleMouseWheel(120, mouse, currentGrid, true, Constants.ZoomSensitivityPercentageDefault, viewport, false, coord);

            // Grid size should change
            Assert.NotEqual(currentGrid, result.NewGridSize);

            var pre = coord.ScreenToFractionalGrid(mouse, currentGrid);
            var post = coord.ScreenToFractionalGrid(mouse, result.NewGridSize);
            var expectedDiffX = pre.X - post.X;
            var expectedDiffY = pre.Y - post.Y;

            Assert.Equal(expectedDiffX, result.NewViewportLeft, 5);
            Assert.Equal(expectedDiffY, result.NewViewportTop, 5);
        }
    }
}
