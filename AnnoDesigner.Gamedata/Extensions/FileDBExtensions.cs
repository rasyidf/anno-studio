using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using FileDBSerializing;

namespace AnnoDesigner.Gamedata
{
    internal static class FileDBExtensions
    {
        internal static Attrib Attribute(this Tag parent, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return parent.Children.Find<Attrib>(name, comparison);
        }

        internal static Attrib Attribute(this IFileDBDocument document, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return document.Roots.Find<Attrib>(name, comparison);
        }

        internal static IEnumerable<Attrib> Attributes(this Tag parent)
        {
            return parent.Children.FindAll<Attrib>();
        }

        internal static IEnumerable<Attrib> Attributes(this IFileDBDocument document)
        {
            return document.Roots.FindAll<Attrib>();
        }

        internal static IEnumerable<Attrib> Attributes(this Tag parent, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return parent.Children.FindAll<Attrib>(name, comparison);
        }

        internal static IEnumerable<Attrib> Attributes(this IFileDBDocument document, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return document.Roots.FindAll<Attrib>(name, comparison);
        }

        internal static Tag Tag(this Tag parent, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return parent.Children.Find<Tag>(name, comparison);
        }

        internal static Tag Tag(this IFileDBDocument document, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return document.Roots.Find<Tag>(name, comparison);
        }

        internal static IEnumerable<Tag> Tags(this Tag parent)
        {
            return parent.Children.FindAll<Tag>();
        }

        internal static IEnumerable<Tag> Tags(this IFileDBDocument document)
        {
            return document.Roots.FindAll<Tag>();
        }

        internal static IEnumerable<Tag> Tags(this Tag parent, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return parent.Children.FindAll<Tag>(name, comparison);
        }

        internal static IEnumerable<Tag> Tags(this IFileDBDocument document, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return document.Roots.FindAll<Tag>(name, comparison);
        }

        internal static bool ToBoolean(this Attrib attrib)
        {
            return Convert.ToBoolean(attrib.Content.Single());
        }

        internal static Dictionary<TKey, Tag> ToDictionary<TKey>(this Tag parent) where TKey : struct, INumber<TKey>
        {
            var result = new Dictionary<TKey, Tag>();
            int size = Marshal.SizeOf<TKey>();

            for (int i = 0; i < parent.Children.Count; i += 2)
            {
                Attrib attrib = (Attrib)parent.Children[i + 0];
                if (attrib.Content.Length != size) throw new InvalidOperationException();
                result.Add(attrib.ToNumber<TKey>(), (Tag)parent.Children[i + 1]);
            }

            return result;
        }

        internal static IFileDBDocument ToFileDBDocument(this Attrib attrib)
        {
            Console.WriteLine($"Loading {attrib.Parent.Name}...");
            using Stream inputStream = attrib.ContentToStream();
            FileDBDocumentVersion version = VersionDetector.GetCompressionVersion(inputStream);
            DocumentParser parser = new DocumentParser(version);
            return parser.LoadFileDBDocument(inputStream);
        }

        internal static IEnumerable<byte> ToNibbles(this Attrib attrib)
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadNibbles();
        }

        internal static T ToNumber<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadNumber<T>();
        }

        internal static IEnumerable<T> ToNumbers<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadAll(reader.ReadNumber<T>);
        }

        internal static IEnumerable<T> ToNumbers<T>(this Attrib attrib, int count) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.Read(count, reader.ReadNumber<T>);
        }

        internal static Point2D<T> ToPoint2D<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadPoint2D<T>();
        }

        internal static Point3D<T> ToPoint3D<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadPoint3D<T>();
        }

        internal static IEnumerable<Point2D<T>> ToPoints2D<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadAll(reader.ReadPoint2D<T>);
        }

        internal static IEnumerable<Point3D<T>> ToPoints3D<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadAll(reader.ReadPoint3D<T>);
        }

        internal static Rectangle<T> ToRectangle<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadRectangle<T>();
        }

        internal static Size<T> ToSize<T>(this Attrib attrib) where T : struct, INumber<T>
        {
            BinaryReader reader = new BinaryReader(attrib.ContentToStream());
            return reader.ReadSize<T>();
        }

        internal static string ToUnicode(this Attrib attrib)
        {
            return Encoding.Unicode.GetString(attrib.Content);
        }

        #region Private Helper Methods

        private static T Find<T>(this IEnumerable<FileDBNode> nodes, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase) where T : FileDBNode
        {
            return nodes.FindAll<T>().SingleOrDefault(node => node.Name.Equals(name, comparison));
        }

        private static IEnumerable<T> FindAll<T>(this IEnumerable<FileDBNode> nodes, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase) where T : FileDBNode
        {
            return nodes.FindAll<T>().Where(node => node.Name.Equals(name, comparison));
        }

        private static IEnumerable<T> FindAll<T>(this IEnumerable<FileDBNode> nodes) where T : FileDBNode
        {
            return nodes.OfType<T>();
        }

        private static Point2D<T> ReadPoint2D<T>(this BinaryReader reader) where T : struct, INumber<T>
        {
            T x = reader.ReadNumber<T>();
            T y = reader.ReadNumber<T>();
            return new Point2D<T>(x, y);
        }

        private static Point3D<T> ReadPoint3D<T>(this BinaryReader reader) where T : struct, INumber<T>
        {
            // order in FileDB is x, z, y
            T x = reader.ReadNumber<T>();
            T z = reader.ReadNumber<T>();
            T y = reader.ReadNumber<T>();
            return new Point3D<T>(x, y, z);
        }

        private static Rectangle<T> ReadRectangle<T>(this BinaryReader reader) where T : struct, INumber<T>
        {
            T x0 = reader.ReadNumber<T>();
            T y0 = reader.ReadNumber<T>();
            T x1 = reader.ReadNumber<T>();
            T y1 = reader.ReadNumber<T>();
            return new Rectangle<T>(x0, y0, x1 - x0, y1 - y0);
        }

        private static Size<T> ReadSize<T>(this BinaryReader reader) where T : struct, INumber<T>
        {
            T width = reader.ReadNumber<T>();
            T height = reader.ReadNumber<T>();
            return new Size<T>(width, height);
        }

        #endregion
    }
}
