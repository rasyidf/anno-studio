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
    internal class TileObject : BaseObject
    {
        public const float Size = 1.0f;

        public TileObject(BuildingInfo template, Point2D<float> position, byte quadrants)
            : this(template, position, 0.0, quadrants)
        {
        }

        public TileObject(BuildingInfo template, Point2D<float> position, double rotation, byte quadrants)
            : base(template, rotation, position)
        {
            this.Quadrants = quadrants;
        }

        /// <summary>
        /// Tile quadrants in bits (4-bit value), for example<br/>
        /// 0x6 (0110) for a ◢ tile<br/>
        /// 0xC (1100) for a ◥ tile<br/>
        /// 0x3 (0011) for a ◣ tile<br/>
        /// 0x9 (1001) for a ◤ tile<br/>
        /// 0xF (1111) for a ■ tile<br/>
        /// </summary>
        public byte Quadrants { get; set; }

        public override AnnoObject CreateObject()
        {
            AnnoObject result = BuildTemplate();
            int rotationDegrees = (int)Math.Round(Rotation * 180 / Math.PI);
            result.Color = Color ?? ColorPresetsHelper.Instance.GetPredefinedColor(result) ?? Colors.Red;
            result.Rotation = Rotation;

            bool isDiagonal = (rotationDegrees + 45) % 90 == 0;
            double scale = isDiagonal ? MathHelper.GetDiagonalSize(Size) : Size;
            result.RotationCenter = new Point(Size / 2, Size / 2);

            double x = Position.X - result.RotationCenter.X * scale;
            double y = Position.Y - result.RotationCenter.Y * scale;
            result.Position = new Point(x, y); // don't round here

            result.TileQuadrants = Quadrants;
            result.Size = new Size(Size, Size);
            result.Label = Label;
            return result;
        }

        protected virtual AnnoObject BuildTemplate()
        {
            return Template.ToAnnoObject();
        }
    }
}
