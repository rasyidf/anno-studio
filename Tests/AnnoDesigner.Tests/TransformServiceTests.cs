using System.Windows;
using AnnoDesigner.Controls.Canvas.Services;
using Xunit;

namespace AnnoDesigner.Tests
{
    public class TransformServiceTests
    {
        [Fact]
        public void DefaultZoom_IdentityRoundtrip()
        {
            var svc = new TransformService();
            Assert.Equal(1.0, svc.Zoom);

            var p = new Point(10, 20);
            var s = svc.CanvasToScreen(p);
            Assert.Equal(p, s);

            var back = svc.ScreenToCanvas(s);
            Assert.Equal(p, back);
        }

        [Fact]
        public void ZoomScaling_ConvertsCorrectly()
        {
            var svc = new TransformService { Zoom = 2.0 };

            var canvasPoint = new Point(3, 4);
            var screen = svc.CanvasToScreen(canvasPoint);
            Assert.Equal(new Point(6, 8), screen);

            var screenPoint = new Point(10, 20);
            var canvas = svc.ScreenToCanvas(screenPoint);
            Assert.Equal(new Point(5, 10), canvas);
        }
    }
}
