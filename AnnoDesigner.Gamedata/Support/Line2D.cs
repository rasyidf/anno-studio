using System;
using System.Collections.Generic;
using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal readonly struct Line2D<T> where T : struct, INumber<T>
    {
        private static readonly T TWO = T.CreateChecked(2);

        public Line2D(T X1, T Y1, T X2, T Y2)
        {
            this.X1 = X1;
            this.Y1 = Y1;
            this.X2 = X2;
            this.Y2 = Y2;

            T dx = X2 - X1;
            T dy = Y2 - Y1;
            this.Angle = Math.Atan2(double.CreateChecked(dy), double.CreateChecked(dx));
            this.Length = Math.Sqrt(double.CreateChecked(dx * dx + dy * dy));
        }

        public Line2D(Point2D<T> start, Point2D<T> end)
            : this(start.X, start.Y, end.X, end.Y)
        {
        }

        public T X1 { get; }
        public T Y1 { get; }
        public T X2 { get; }
        public T Y2 { get; }

        public double Angle { get; }
        public double Length { get; }

        public IEnumerable<Point2D<T>> Rasterize()
        {
            return Rasterize(X1 < X2 ? T.One : -T.One, Y1 < Y2 ? T.One : -T.One);
        }

        public IEnumerable<Point2D<T>> Rasterize(T sx, T sy)
        {
            T dx =  T.Abs(X2 - X1);
            T dy = -T.Abs(Y2 - Y1);
            T error = dx + dy;

            T x = X1;
            T y = Y1;

            while (true)
            {
                yield return new Point2D<T>(x, y);
                T e2 = TWO * error;

                if (e2 >= dy)
                {
                    if (x == X2) break;
                    error += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    if (y == Y2) break;
                    error += dx;
                    y += sy;
                }
            }
        }
    }
}
