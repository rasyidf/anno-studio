using System;
using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal static class SupportExtensions
    {
        internal static Point2D<int> Round<T>(this Point2D<T> input)
            where T : struct, IFloatingPoint<T>
        {
            int X = int.CreateChecked(T.Round(input.X));
            int Y = int.CreateChecked(T.Round(input.Y));
            return new Point2D<int>(X, Y);
        }

        internal static Point2D<int> Round<T>(this Point2D<T> input, MidpointRounding mode)
            where T : struct, IFloatingPoint<T>
        {
            int X = int.CreateChecked(T.Round(input.X, mode));
            int Y = int.CreateChecked(T.Round(input.Y, mode));
            return new Point2D<int>(X, Y);
        }

        internal static Point2D<R> Scale<T, R>(this Point2D<T> input, R factor)
            where T : struct, INumber<T>
            where R : struct, INumber<R>
        {
            R X = R.CreateChecked(input.X) * factor;
            R Y = R.CreateChecked(input.Y) * factor;
            return new Point2D<R>(X, Y);
        }

        internal static Grid2D<bool> Intersect(this Grid2D<bool> input, Grid2D<bool> other)
        {
            if (input.Width != other.Width || input.Height != other.Height) throw new ArgumentException("Grid sizes do not match.");
            return new Grid2D<bool>(input.Width, input.Height, (x, y) => input[x, y] && other[x, y]);
        }

        internal static Grid2D<bool> Subtract(this Grid2D<bool> input, Grid2D<bool> other)
        {
            if (input.Width != other.Width || input.Height != other.Height) throw new ArgumentException("Grid sizes do not match.");
            return new Grid2D<bool>(input.Width, input.Height, (x, y) => input[x, y] && !other[x, y]);
        }

        internal static Grid2D<bool> ToBoolean<T>(this Grid2D<T> input, Predicate<T> predicate)
        {
            return new Grid2D<bool>(input.Width, input.Height, (x, y) => predicate(input[x, y]));
        }

        internal static Grid2D<bool> ToOutline(this Grid2D<bool> input)
        {
            Grid2D<bool> result = new Grid2D<bool>(input.Width, input.Height);

            for (int y = 0; y < input.Height; y++)
            {
                for (int x = 0; x < input.Width; x++)
                {
                    if (input[x, y])
                    {
                        for (int ny = y - 1; ny <= y + 1; ny++)
                        {
                            for (int nx = x - 1; nx <= x + 1; nx++)
                            {
                                if (nx >= 0 && nx < input.Width && ny >= 0 && ny < input.Height)
                                {
                                    if (!input[nx, ny])
                                    {
                                        result[nx, ny] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
