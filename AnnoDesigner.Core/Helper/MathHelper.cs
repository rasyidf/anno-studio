using System;

namespace AnnoDesigner.Core.Helper
{
    public static class MathHelper
    {
        private static readonly double sqrt2 = Math.Sqrt(2);
        private static readonly double subTileSize = 1 / sqrt2;

        /// <summary>
        /// Return the fractional value of a <see cref="double"/>.
        /// This value will always be between -0.99 recurring and 0.99 recurring.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double FractionalValue(double value) => value - Math.Truncate(value);

        /// <summary>
        /// Computes the <paramref name="N"/>th root of a number.
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/questions/18657508/c-sharp-find-nth-root
        /// </remarks>
        /// <param name="A"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        public static double NthRoot(double A, double N) => Math.Pow(A, 1.0 / N);

        /// <summary>
        /// Diagonal grid sizing for Anno 117.
        /// See: https://www.anno-union.com/devblog-roads-building-in-the-grid/
        /// </summary>
        public static double GetDiagonalSize(double size)
        {
            if (size == 1) return sqrt2;
            double sizeShrink = (int)Math.Floor(size / subTileSize) * subTileSize;
            double sizeExpand = (int)Math.Ceiling(size / subTileSize) * subTileSize;
            return Math.Abs(size - sizeShrink) < Math.Abs(size - sizeExpand) ? sizeShrink : sizeExpand;
        }
    }
}
