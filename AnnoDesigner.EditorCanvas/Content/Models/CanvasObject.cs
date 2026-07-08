using System;
using System.Collections.Generic;
using System.Windows;

namespace AnnoDesigner.Controls.EditorCanvas.Content.Models
{
    /// <summary>
    /// Minimal canvas object model for EditorCanvas scaffolding.
    /// Real layout objects (buildings, influences, icons) will extend this model.
    /// </summary>
    public class CanvasObject : ICanvasObject
    {
        public Guid Id { get; } = Guid.NewGuid();

        public virtual Rect Bounds { get; set; }

        public bool IsSelectable { get; set; } = true;

        public virtual string Identifier { get; set; }

        /// <summary>
        /// Z-order index. Higher values are drawn on top and considered first in hit-tests.
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>Rotation in degrees applied around the object's center.</summary>
        public double RotationDegrees { get; set; } = 0;

        /// <inheritdoc />
        public object? Tag { get; set; }

        /// <summary>Shape type hint for rendering.</summary>
        public string ShapeType { get; set; } = "Rectangle";

        /// <summary>For Line shapes: start point (relative to Bounds origin).</summary>
        public Point? LineStart { get; set; }

        /// <summary>For Line shapes: end point (relative to Bounds origin).</summary>
        public Point? LineEnd { get; set; }

        /// <summary>For Pencil/freeform shapes: list of points (in world coordinates).</summary>
        public List<Point>? PathPoints { get; set; }

        /// <summary>For Curve shapes: cubic bezier control points (world coordinates).</summary>
        public List<(Point Point, Point? ControlIn, Point? ControlOut)>? BezierPoints { get; set; }

        public CanvasObject Clone()
        {
            return new CanvasObject
            {
                Bounds = this.Bounds,
                IsSelectable = this.IsSelectable,
                Identifier = this.Identifier,
                ZIndex = this.ZIndex,
                RotationDegrees = this.RotationDegrees,
                Tag = this.Tag,
                ShapeType = this.ShapeType,
                LineStart = this.LineStart,
                LineEnd = this.LineEnd,
                PathPoints = this.PathPoints != null ? new List<Point>(this.PathPoints) : null,
                BezierPoints = this.BezierPoints != null ? new List<(Point, Point?, Point?)>(this.BezierPoints) : null
            };
        }

        /// <summary>
        /// Offsets all geometry (Bounds, LineStart, LineEnd, PathPoints, BezierPoints) by the given vector.
        /// This is the single place that understands how to translate all shape types.
        /// </summary>
        public void OffsetBy(Vector delta)
        {
            Bounds = new Rect(Bounds.X + delta.X, Bounds.Y + delta.Y, Bounds.Width, Bounds.Height);

            if (LineStart.HasValue)
                LineStart = new Point(LineStart.Value.X + delta.X, LineStart.Value.Y + delta.Y);
            if (LineEnd.HasValue)
                LineEnd = new Point(LineEnd.Value.X + delta.X, LineEnd.Value.Y + delta.Y);

            if (PathPoints != null)
            {
                for (int i = 0; i < PathPoints.Count; i++)
                    PathPoints[i] = new Point(PathPoints[i].X + delta.X, PathPoints[i].Y + delta.Y);
            }

            if (BezierPoints != null)
            {
                for (int i = 0; i < BezierPoints.Count; i++)
                {
                    var (pt, ci, co) = BezierPoints[i];
                    BezierPoints[i] = (
                        new Point(pt.X + delta.X, pt.Y + delta.Y),
                        ci.HasValue ? new Point(ci.Value.X + delta.X, ci.Value.Y + delta.Y) : null,
                        co.HasValue ? new Point(co.Value.X + delta.X, co.Value.Y + delta.Y) : null
                    );
                }
            }
        }
    }
}
