using System;
using System.Collections.Generic;
using System.Linq;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Models;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Default implementation of <see cref="ITransformationService"/>.
    /// Operates on `LayoutObject` instances and updates their positions/sizes only (no UI concerns).
    /// </summary>
    public class TransformationService : ITransformationService
    {
        public void Align(IEnumerable<object> items, AlignmentMode mode)
        {
            var objs = items?.OfType<LayoutObject>().ToList();
            if (objs == null || objs.Count < 2) return;

            double reference;
            switch (mode)
            {
                case AlignmentMode.Left:
                    reference = objs.Min(o => o.Position.X);
                    foreach (var o in objs) o.Position = new System.Windows.Point(reference, o.Position.Y);
                    break;
                case AlignmentMode.Right:
                    reference = objs.Max(o => o.Position.X + o.Size.Width);
                    foreach (var o in objs) o.Position = new System.Windows.Point(reference - o.Size.Width, o.Position.Y);
                    break;
                case AlignmentMode.Center:
                    reference = objs.Average(o => o.Position.X + (o.Size.Width / 2.0));
                    foreach (var o in objs) o.Position = new System.Windows.Point(reference - (o.Size.Width / 2.0), o.Position.Y);
                    break;
                case AlignmentMode.Top:
                    reference = objs.Min(o => o.Position.Y);
                    foreach (var o in objs) o.Position = new System.Windows.Point(o.Position.X, reference);
                    break;
                case AlignmentMode.Bottom:
                    reference = objs.Max(o => o.Position.Y + o.Size.Height);
                    foreach (var o in objs) o.Position = new System.Windows.Point(o.Position.X, reference - o.Size.Height);
                    break;
                case AlignmentMode.Middle:
                    reference = objs.Average(o => o.Position.Y + (o.Size.Height / 2.0));
                    foreach (var o in objs) o.Position = new System.Windows.Point(o.Position.X, reference - (o.Size.Height / 2.0));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public void Distribute(IEnumerable<object> items, DistributionMode mode)
        {
         
            if (mode == DistributionMode.Horizontal)
            {
                
            var objsX = items?.OfType<LayoutObject>().OrderBy(o => o.Position.X).ToList();
            if (objsX == null || objsX.Count < 3) return; // need at least 3 to distribute

                var sorted = objsX.OrderBy(o => o.Position.X).ToList();
                var left = sorted.First();
                var right = sorted.Last();
                var totalSpan = right.Position.X + right.Size.Width - left.Position.X;
                var usedWidth = sorted.Sum(o => o.Size.Width);
                var gap = (totalSpan - usedWidth) / (sorted.Count - 1);
                double curX = left.Position.X;
                foreach (var o in sorted)
                {
                    o.Position = new System.Windows.Point(curX, o.Position.Y);
                    curX += o.Size.Width + gap;
                }
            }
            else // Vertical
            {   var objsY = items?.OfType<LayoutObject>().OrderBy(o => o.Position.Y).ToList();

                if (objsY == null || objsY.Count < 3) return; // need at least 3 to distribute
                var sorted = objsY.OrderBy(o => o.Position.Y).ToList();
                var top = sorted.First();
                var bottom = sorted.Last();
                var totalSpan = bottom.Position.Y + bottom.Size.Height - top.Position.Y;
                var usedHeight = sorted.Sum(o => o.Size.Height);
                var gap = (totalSpan - usedHeight) / (sorted.Count - 1);
                var curY = top.Position.Y;
                foreach (var o in sorted)
                {
                    o.Position = new System.Windows.Point(o.Position.X, curY);
                    curY += o.Size.Height + gap;
                }
            }
        }


        public void Rotate(IEnumerable<object> items, RotationDirection direction)
        {
            var objs = items?.OfType<LayoutObject>().ToList();
            if (objs == null || objs.Count == 0) return;

            // Compute group bounding box center
            var left = objs.Min(o => o.Position.X);
            var right = objs.Max(o => o.Position.X + o.Size.Width);
            var top = objs.Min(o => o.Position.Y);
            var bottom = objs.Max(o => o.Position.Y + o.Size.Height);
            var cx = (left + right) / 2.0;
            var cy = (top + bottom) / 2.0;

            // 90 degree rotation: clockwise -> -90deg, counterclockwise -> +90deg
            var angle = direction == RotationDirection.Clockwise ? -Math.PI / 2.0 : Math.PI / 2.0;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            foreach (var o in objs)
            {
                var center = new System.Windows.Point(o.Position.X + (o.Size.Width / 2.0), o.Position.Y + (o.Size.Height / 2.0));
                var dx = center.X - cx;
                var dy = center.Y - cy;

                var nx = cx + (cos * dx) - (sin * dy);
                var ny = cy + (sin * dx) + (cos * dy);

                var newSize = new System.Windows.Size(o.Size.Height, o.Size.Width);
                o.Size = newSize;
                o.Position = new System.Windows.Point(nx - (newSize.Width / 2.0), ny - (newSize.Height / 2.0));
            }
        }

        public void Flip(IEnumerable<object> items, FlipDirection direction)
        {
            var objs = items?.OfType<LayoutObject>().ToList();
            if (objs == null || objs.Count == 0) return;

            // Flip around the bounding box center
            var left = objs.Min(o => o.Position.X);
            var right = objs.Max(o => o.Position.X + o.Size.Width);
            var top = objs.Min(o => o.Position.Y);
            var bottom = objs.Max(o => o.Position.Y + o.Size.Height);
            var centerX = (left + right) / 2.0;
            var centerY = (top + bottom) / 2.0;

            foreach (var o in objs)
            {
                if (direction == FlipDirection.Horizontal)
                {
                    var dist = o.Position.X + (o.Size.Width / 2.0) - centerX;
                    var newCenterX = centerX - dist;
                    o.Position = new System.Windows.Point(newCenterX - (o.Size.Width / 2.0), o.Position.Y);
                }
                else
                {
                    var dist = (o.Position.Y + (o.Size.Height / 2.0)) - centerY;
                    var newCenterY = centerY - dist;
                    o.Position = new System.Windows.Point(o.Position.X, newCenterY - (o.Size.Height / 2.0));
                }
            }
        }

    }
}
