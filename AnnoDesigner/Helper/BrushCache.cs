using System.Collections.Generic;
using System.Windows.Media;
using AnnoDesigner.Models;

namespace AnnoDesigner.Helper
{
    public class BrushCache : IBrushCache
    {
        private static readonly Dictionary<Color, SolidColorBrush> _cachedBrushes;

        static BrushCache()
        {
            _cachedBrushes = [];
        }

        public SolidColorBrush GetSolidBrush(Color color)
        {
            if (!_cachedBrushes.TryGetValue(color, out var foundBrush))
            {
                foundBrush = new SolidColorBrush(color);
                if (foundBrush.CanFreeze)
                {
                    foundBrush.Freeze();
                }

                _cachedBrushes.Add(color, foundBrush);
            }

            return foundBrush;
        }
    }
}
