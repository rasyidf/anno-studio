using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal readonly struct Size<T> where T : struct, INumber<T>
    {
        public Size(T width, T height)
        {
            this.Height = height;
            this.Width = width;
        }

        public T Height { get; }
        public T Width { get; }
    }
}
