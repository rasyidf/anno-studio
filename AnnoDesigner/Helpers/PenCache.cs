using System.Collections.Generic;
using System.Windows.Media;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Helper
{
    public class PenCache : IPenCache
    {
        private static readonly Dictionary<(Brush brush, double thickness), Pen> _cachedPens;

        static PenCache()
        {
            _cachedPens = [];
        }

        public Pen GetPen(Brush brush, double thickness)
        {
            if (!_cachedPens.TryGetValue((brush, thickness), out var foundPen))
            {
                foundPen = new Pen(brush, thickness);
                if (foundPen.CanFreeze)
                {
                    foundPen.Freeze();
                }

                _cachedPens.Add((brush, thickness), foundPen);
            }

            return foundPen;
        }
    }
}
