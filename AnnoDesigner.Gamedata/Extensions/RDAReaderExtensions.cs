using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FileDBSerializing;
using RDAExplorer;

namespace AnnoDesigner.Gamedata
{
    internal static class RDAReaderExtensions
    {
        internal static RDAFolder Folder(this RDAReader reader)
        {
            if (reader.rdaReadBlocks == 0) reader.ReadRDAFile();
            return reader.rdaFolder;
        }

        internal static RDAFile File(this RDAReader reader, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return reader.Folder().Files.SingleOrDefault(file => file.FileName.Equals(name, comparison));
        }

        internal static IEnumerable<RDAFile> EnumerateFiles(this RDAReader reader, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (searchOption == SearchOption.AllDirectories) return reader.Folder().GetAllFiles();
            else return reader.Folder().Files;
        }

        internal static IFileDBDocument GetFileDBDocument(this RDAFile file)
        {
            using (Stream inputStream = new MemoryStream(file.GetData()))
            {
                FileDBDocumentVersion version = VersionDetector.GetCompressionVersion(inputStream);
                DocumentParser parser = new DocumentParser(version);
                return parser.LoadFileDBDocument(inputStream);
            }
        }

        internal static IFileDBDocument GetFileDBDocumentInflated(this RDAFile file)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                using (Stream inputStream = new MemoryStream(file.GetData()))
                {
                    using (Stream inflater = new ZLibStream(inputStream, CompressionMode.Decompress))
                    {
                        inflater.CopyTo(buffer);
                        buffer.Position = 0;
                    }
                }

                FileDBDocumentVersion version = VersionDetector.GetCompressionVersion(buffer);
                DocumentParser parser = new DocumentParser(version);
                return parser.LoadFileDBDocument(buffer);
            }
        }
    }
}
