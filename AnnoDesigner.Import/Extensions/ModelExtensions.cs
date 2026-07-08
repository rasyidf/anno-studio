using System;
using System.Collections.Generic;
using AnnoDesigner.Gamedata;
using FileDBSerializing;

namespace AnnoDesigner.Import
{
    internal static class ModelExtensions
    {
        internal static bool ContainsAll(this Rectangle<int> rectangle, IEnumerable<Tag> gameObjects)
        {
            foreach (Tag gameObject in gameObjects)
            {
                var attribute = gameObject.Attribute("Position");
                if (attribute == null) throw new ArgumentException("Invalid game object!");

                var position = attribute.ToPoint3D<float>();
                if (!rectangle.Contains((int)position.X, (int)position.Y)) return false;
            }

            return true;
        }
    }
}
