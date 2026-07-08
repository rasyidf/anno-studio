using System;
using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal readonly struct Rectangle<T> where T : struct, INumber<T>
    {
        public Rectangle(T x, T y, T width, T height)
        {
            this.X = x;
            this.Y = y;
            this.Height = height;
            this.Width = width;
        }

        public Rectangle(Point2D<T> position, Size<T> size)
            : this(position.X, position.Y, size.Width, size.Height)
        {
        }

        public T X { get; }
        public T Y { get; }
        public T Height { get; }
        public T Width { get; }

        public bool Contains(T x, T y)
        {
            return X <= x && x < X + Width && Y <= y && y < Y + Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Height, Width);
        }

        public override bool Equals(object obj)
        {
            return obj is Rectangle<T> other &&
                X.Equals(other.X) && Y.Equals(other.Y) &&
                Height.Equals(other.Height) &&
                Width.Equals(other.Width);
        }
    }
}
