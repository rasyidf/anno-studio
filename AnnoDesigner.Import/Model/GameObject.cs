using System;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Helper;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Gamedata;

namespace AnnoDesigner.Import.Model
{
    internal class GameObject : BaseObject
    {
        private const string X = "x0";
        private const string Y = "z0";
        private const string Width = "x";
        private const string Height = "z";

        public GameObject(BuildingInfo template, double rotation, Point2D<float> position)
            : base(template, rotation, position)
        {
            this.BuildBlocker = SelectFootprint(template, position);
        }

        public Rectangle<double> BuildBlocker { get; }

        private static double DistanceToWholeTile(double value) => Math.Abs(value - Math.Round(value));

        /// <summary>
        /// Picks the footprint that lands the building on whole tiles. A building can have several
        /// size variants (e.g. a land and a marsh version), only the matching one puts the pivot on
        /// the grid without rounding.
        /// </summary>
        private static Rectangle<double> SelectFootprint(BuildingInfo template, Point2D<float> pivot)
        {
            Rectangle<double> ToRect(SerializableDictionary<double> bb) =>
                new Rectangle<double>(bb[X], bb[Y], bb[Width], bb[Height]);

            // orthogonal baked corner: left = pivot.X - (Width + x0), top = pivot.Y + z0
            double FractionError(Rectangle<double> r) =>
                DistanceToWholeTile(pivot.X - (r.Width + r.X)) + DistanceToWholeTile(pivot.Y + r.Y);

            var best = ToRect(template.BuildBlocker);
            if (template.BuildBlockerVariants == null || template.BuildBlockerVariants.Count == 0)
            {
                return best;
            }

            var bestError = FractionError(best);
            foreach (var variant in template.BuildBlockerVariants)
            {
                if (variant == null || variant[Width] <= 0 || variant[Height] <= 0)
                {
                    continue;
                }

                var candidate = ToRect(variant);
                var error = FractionError(candidate);
                if (error < bestError - 1e-6)
                {
                    bestError = error;
                    best = candidate;
                }
            }

            return best;
        }

        public override AnnoObject CreateObject()
        {
            AnnoObject result = Template.ToAnnoObject();
            int rotationDegrees = Round(Rotation * 180 / Math.PI);
            result.Color = Color ?? ColorPresetsHelper.Instance.GetPredefinedColor(result) ?? Colors.Red;
            result.Direction = (GridDirection)(Round(Rotation / Math.PI * 2) % 4);

            // the saved position is the rotation pivot. some buildings are off-center so the pivot
            // comes from the BuildBlocker offsets, not width/2,height/2
            var rotationCenter = new Point(BuildBlocker.Width + BuildBlocker.X, -BuildBlocker.Y); // don't round here

            if (rotationDegrees % 90 != 0)
            {
                // diagonal building, keep the render-time rotation. its footprint is a diamond and
                // can't be an axis-aligned rect
                result.Rotation = Rotation;
                result.RotationCenter = rotationCenter;

                // scale everything into the diagonal grid because diagonal tiles are larger
                double scaleX = MathHelper.GetDiagonalSize(BuildBlocker.Width) / BuildBlocker.Width;
                double scaleY = MathHelper.GetDiagonalSize(BuildBlocker.Height) / BuildBlocker.Height;
                result.Position = new Point(Position.X - rotationCenter.X * scaleX, Position.Y - rotationCenter.Y * scaleY); // don't round here
                result.Size = new Size((int)BuildBlocker.Width, (int)BuildBlocker.Height);
            }
            else
            {
                // orthogonal building, bake the rotation into Position + Size so the stored footprint
                // matches what gets drawn. collision, road search and influence all use the grid rect
                double left = Position.X - rotationCenter.X;
                double top = Position.Y - rotationCenter.Y;
                double w = BuildBlocker.Width;
                double h = BuildBlocker.Height;

                // rotate the footprint corners around the pivot, take the axis-aligned bounds
                var matrix = new Matrix();
                matrix.RotateAt(-rotationDegrees, Position.X, Position.Y);
                double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
                foreach (var corner in new[] { new Point(left, top), new Point(left + w, top), new Point(left + w, top + h), new Point(left, top + h) })
                {
                    var p = matrix.Transform(corner);
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                }

                // with the matched footprint this is already whole, the rounding just covers the rare
                // building whose variant we couldn't match
                result.Rotation = 0;
                result.Position = new Point(
                    Math.Round(minX, MidpointRounding.AwayFromZero),
                    Math.Round(minY, MidpointRounding.AwayFromZero));
                result.Size = new Size((int)Math.Round(maxX - minX), (int)Math.Round(maxY - minY));
            }

            result.Label = Label;
            return result;
        }
    }
}
