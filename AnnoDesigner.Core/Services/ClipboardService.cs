using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using Newtonsoft.Json;

namespace AnnoDesigner.Core.Services
{
    public class ClipboardService(ILayoutLoader layoutLoaderToUse, IClipboard clipboardToUse) : IClipboardService
    {
        public void Copy(IEnumerable<AnnoObject> objects)
        {
            if (objects is not null && objects.Any())
            {
                using var memoryStream = new MemoryStream();
                layoutLoaderToUse.SaveLayout(new LayoutFile(objects), memoryStream);
                _ = memoryStream.Seek(0, SeekOrigin.Begin);
                clipboardToUse.Clear();
                clipboardToUse.SetData(CoreConstants.AnnoDesignerClipboardFormat, memoryStream);
                clipboardToUse.Flush();
            }
        }

        public ICollection<AnnoObject> Paste()
        {
            var files = clipboardToUse.GetFileDropList();
            if (files?.Count == 1)
            {
                try
                {
                    return layoutLoaderToUse.LoadLayout(files[0], forceLoad: true).Objects;
                }
                catch (JsonReaderException) { }
            }

            if (clipboardToUse.ContainsData(CoreConstants.AnnoDesignerClipboardFormat))
            {
                try
                {
                    var stream = clipboardToUse.GetData(CoreConstants.AnnoDesignerClipboardFormat) as Stream;
                    if (stream is not null)
                    {
                        return layoutLoaderToUse.LoadLayout(stream, forceLoad: true).Objects;
                    }
                }
                catch (JsonReaderException) { }
            }

            if (clipboardToUse.ContainsText())
            {
                using var memoryStream = new MemoryStream();
                using var streamWriter = new StreamWriter(memoryStream);
                streamWriter.Write(clipboardToUse.GetText());
                streamWriter.Flush();
                _ = memoryStream.Seek(0, SeekOrigin.Begin);
                try
                {
                    return layoutLoaderToUse.LoadLayout(memoryStream, forceLoad: true).Objects;
                }
                catch (JsonReaderException) { }
            }

            return Array.Empty<AnnoObject>();
        }
    }
}
