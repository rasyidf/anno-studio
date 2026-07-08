using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Gamedata;

namespace AnnoDesigner.Import.Model
{
    internal class Island
    {
        public Island(string cityName, string template, Point2D<int> position, GridDirection rotation, Size<int> size, ZipArchiveEntry outlines)
        {
            this.CityName = cityName;
            this.Colors = new Dictionary<long, SerializableColor>();
            this.Objects = new List<AnnoObject>();
            this.Template = template;

            this.Position = position;
            this.Rotation = rotation;
            this.Size = size;

            if (outlines != null)
            {
                Objects.AddRange(LoadOutlines(outlines));
            }
        }

        public string CityName { get; }
        public string Template { get; }

        public Point2D<int> Position { get; }
        public GridDirection Rotation { get; }
        public Size<int> Size { get; }

        public Dictionary<long, SerializableColor> Colors { get; }
        public List<AnnoObject> Objects { get; }

        private List<AnnoObject> LoadOutlines(ZipArchiveEntry outlines)
        {
            using (Stream inputStream = outlines.Open())
            {
                LayoutFile layout = SerializationHelper.LoadFromStream<LayoutFile>(inputStream);

                foreach (var obj in layout.Objects)
                {
                    double x = obj.Position.X;
                    double y = obj.Position.Y;

                    switch (this.Rotation)
                    {
                        case GridDirection.Up: // 0 degrees
                            x = this.Size.Width - obj.Position.X - 1;
                            y = obj.Position.Y;
                            break;
                        case GridDirection.Right: // 90 degrees (default orientation of outline files)
                            x = obj.Position.Y;
                            y = obj.Position.X;
                            break;
                        case GridDirection.Down: // 180 degrees
                            x = obj.Position.X;
                            y = this.Size.Height - obj.Position.Y - 1;
                            break;
                        case GridDirection.Left: // 270 degrees
                            x = this.Size.Height - obj.Position.Y - 1;
                            y = this.Size.Width - obj.Position.X - 1;
                            break;
                    }

                    obj.Position = new System.Windows.Point(x, y);

                    if (obj.TileQuadrants.HasValue)
                    {
                        obj.TileQuadrants = RotateQuadrants(obj.TileQuadrants.Value, this.Rotation);
                    }
                }

                return layout.Objects;
            }
        }

        // outline quadrants are bits 1=W 2=S 4=E 8=N. a rotated island needs the same transform on
        // the quadrants that the positions get above, else the bevels point the wrong way
        private static byte RotateQuadrants(byte q, GridDirection rotation)
        {
            bool w = (q & 1) != 0, s = (q & 2) != 0, e = (q & 4) != 0, n = (q & 8) != 0;
            bool nw, ns, ne, nn;
            switch (rotation)
            {
                case GridDirection.Up:    nw = e; ns = s; ne = w; nn = n; break; // mirror X (swap E,W)
                case GridDirection.Down:  nw = w; ns = n; ne = e; nn = s; break; // mirror Y (swap N,S)
                case GridDirection.Right: nw = n; ns = e; ne = s; nn = w; break; // transpose
                case GridDirection.Left:  nw = s; ns = w; ne = n; nn = e; break; // anti-transpose
                default:                  nw = w; ns = s; ne = e; nn = n; break;
            }
            return (byte)((nw ? 1 : 0) | (ns ? 2 : 0) | (ne ? 4 : 0) | (nn ? 8 : 0));
        }

        public Point2D<float> ToLocalCoordinates<T>(Point2D<T> world) where T : struct, INumber<T>
        {
            float X = this.Size.Width - (float.CreateChecked(world.X) - this.Position.X); // flip horizontally
            float Y = float.CreateChecked(world.Y) - this.Position.Y;
            return new Point2D<float>(X, Y);
        }
    }
}
