using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace AnnoDesigner.Gamedata
{
    internal static class BinaryReaderExtensions
    {
        internal static IEnumerable<T> Read<T>(this BinaryReader reader, int count, Func<T> func)
        {
            for (int i = 0; i < count; i++)
            {
                if (reader.BaseStream.Position < reader.BaseStream.Length) yield return func();
                else throw new EndOfStreamException();
            }
        }

        internal static IEnumerable<T> ReadAll<T>(this BinaryReader reader, Func<T> func)
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                yield return func();
            }
        }

        internal static IEnumerable<byte> ReadNibbles(this BinaryReader reader)
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte value = reader.ReadByte();
                yield return (byte)(value & 0x0F); // lo
                yield return (byte)((value & 0xF0) >> 4); // hi
            }
        }

        internal static T ReadNumber<T>(this BinaryReader reader) where T : struct, INumber<T>
        {
            object result;
            Type targetType = typeof(T);
            if (targetType == typeof(Byte)) result = reader.ReadByte();
            else if (targetType == typeof(SByte)) result = reader.ReadSByte();
            else if (targetType == typeof(Int16)) result = reader.ReadInt16();
            else if (targetType == typeof(UInt16)) result = reader.ReadUInt16();
            else if (targetType == typeof(Int32)) result = reader.ReadInt32();
            else if (targetType == typeof(UInt32)) result = reader.ReadUInt32();
            else if (targetType == typeof(Int64)) result = reader.ReadInt64();
            else if (targetType == typeof(UInt64)) result = reader.ReadUInt64();
            else if (targetType == typeof(Single)) result = reader.ReadSingle();
            else if (targetType == typeof(Double)) result = reader.ReadDouble();
            else if (targetType == typeof(Decimal)) result = reader.ReadDecimal();
            else throw new InvalidOperationException($"Type '{targetType}' not supported!");
            return (T)result;
        }
    }
}
