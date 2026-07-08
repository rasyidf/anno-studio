using System;

namespace AnnoDesigner.Gamedata
{
    internal class Grid2D<T>
    {
        private readonly T[] data;

        public Grid2D(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.data = new T[width * height];
        }

        public Grid2D(int width, int height, Func<int, int, T> factory)
            : this(width, height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    this[x, y] = factory(x, y);
                }
            }
        }

        public Grid2D(T[] data, int width, int height)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length != width * height) throw new ArgumentException("Data length does not match width/height.", nameof(data));
            this.Width = width;
            this.Height = height;
            this.data = data;
        }

        public T this[int x, int y]
        {
            get { return data[y * Width + x]; }
            set { data[y * Width + x] = value; }
        }

        public int Width { get; }
        public int Height { get; }

        public void Copy(Grid2D<T> source, int destinationX, int destinationY)
        {
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    this[destinationX + x, destinationY + y] = source[x, y];
                }
            }
        }

        public void Fill(T value)
        {
            Array.Fill(data, value);
        }
    }
}
