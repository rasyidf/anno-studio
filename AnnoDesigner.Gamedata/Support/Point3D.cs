using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal readonly struct Point3D<T> where T : struct, INumber<T>
    {
        public Point3D(T x, T y, T z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
        
        public T X { get; }
        public T Y { get; }
        public T Z { get; }

        public static implicit operator Point2D<T>(Point3D<T> input)
        {
            return new Point2D<T>(input.X, input.Y);
        }
    }
}
