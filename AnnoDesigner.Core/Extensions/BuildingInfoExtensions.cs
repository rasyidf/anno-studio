using System.Windows;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;

// ponytail: namespace matches the fork so that AnnoDesigner.Import can resolve ToAnnoObject()
// without a dependency on the main AnnoDesigner project.
namespace AnnoDesigner.Core.Extensions
{
    public static class BuildingInfoExtensions
    {
        public static AnnoObject ToAnnoObject(this IBuildingInfo buildingInfo)
        {
            return new AnnoObject
            {
                Label = buildingInfo.Identifier,
                Icon = buildingInfo.IconFileName,
                Radius = buildingInfo.InfluenceRadius,
                InfluenceRange = buildingInfo.InfluenceRange - 2,
                Identifier = buildingInfo.Identifier,
                Size = buildingInfo.BuildBlocker == null ? new Size() : new Size(buildingInfo.BuildBlocker["x"], buildingInfo.BuildBlocker["z"]),
                Template = buildingInfo.Template,
                Road = buildingInfo.Road,
                RoadInfluenceFactor = buildingInfo.RoadInfluenceFactor,
                Borderless = buildingInfo.Borderless,
                BlockedAreaLength = buildingInfo.BlockedAreaLength,
                BlockedAreaWidth = buildingInfo.BlockedAreaWidth,
                Direction = buildingInfo.Direction
            };
        }

        /// <summary>
        /// Creates an <see cref="AnnoObject"/> with a localized label from the building's localization dictionary.
        /// </summary>
        public static AnnoObject ToAnnoObject(this IBuildingInfo buildingInfo, string selectedLanguageCode)
        {
            var result = buildingInfo.ToAnnoObject();
            result.Label = buildingInfo.GetOrderParameter(selectedLanguageCode);
            return result;
        }

        /// <summary>
        /// Resolves the localized display name for a building, falling back to English then Identifier.
        /// </summary>
        public static string GetOrderParameter(this IBuildingInfo buildingInfo, string selectedLanguageCode)
        {
            var labelLocalization = buildingInfo.Localization == null ? buildingInfo.Identifier : buildingInfo.Localization[selectedLanguageCode];
            if (string.IsNullOrEmpty(labelLocalization))
            {
                labelLocalization = buildingInfo.Localization["eng"];
            }

            return labelLocalization;
        }
    }
}
