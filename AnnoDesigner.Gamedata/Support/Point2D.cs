using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal readonly struct Point2D<T> where T : struct, INumber<T>
    {
        public Point2D(T x, T y)
        {
            this.X = x;
            this.Y = y;
        }

        public T X { get; }
        public T Y { get; }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }
    }
}
