using System;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Gamedata;

namespace AnnoDesigner.Import.Model
{
    internal abstract class BaseObject
    {
        protected BaseObject(BuildingInfo template, double rotation, Point2D<float> position)
        {
            this.Color = null;
            this.Template = template;
            this.Label = string.Empty;
            this.Position = position;
            this.Rotation = rotation;
        }

        public BuildingInfo Template { get; }
        public SerializableColor? Color { get; set; }
        public string Label { get; set; }

        public Point2D<float> Position { get; }
        public double Rotation { get; }

        public abstract AnnoObject CreateObject();

        protected static int Round(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
