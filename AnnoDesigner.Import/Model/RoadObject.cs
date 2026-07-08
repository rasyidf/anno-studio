using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Gamedata;

namespace AnnoDesigner.Import.Model
{
    internal class RoadObject : TileObject
    {
        public RoadObject(BuildingInfo template, Point2D<float> position, double rotation, byte quadrants)
            : base(template, position, rotation, quadrants)
        {
        }

        public override AnnoObject CreateObject()
        {
            AnnoObject result = base.CreateObject();
            result.Road = true;
            return result;
        }

        protected override AnnoObject BuildTemplate()
        {
            return Template != null ? Template.ToAnnoObject() : new AnnoObject
            {
                Color = new SerializableColor(255, 169, 169, 169),
                Identifier = "Road",
                Template = "Road",
            };
        }
    }
}
