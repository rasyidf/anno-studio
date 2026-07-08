using System;
using System.IO;
using System.IO.Compression;

namespace AnnoDesigner.Import.Outlines
{
    internal static class OutlinesLoader
    {
        internal static ZipArchive LoadArchive(string name)
        {
            var type = typeof(OutlinesLoader);
            string resourceName = $"{type.Namespace}.{name}.zip";
            Stream inputStream = type.Assembly.GetManifestResourceStream(resourceName);
            if (inputStream == null) throw new InvalidOperationException($"Unable to load outlines of type {name}");
            return new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: false); // stream will be closed when the archive is disposed, so no need for additional using on the inputStream
        }
    }
}
