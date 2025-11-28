using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Handles serialization and deserialization of canvas documents.
/// </summary>
public interface ICanvasSerializer
{
    /// <summary>
    /// Serialize document to JSON string.
    /// </summary>
    string Serialize(CanvasDocument document);

    /// <summary>
    /// Deserialize JSON string to document.
    /// </summary>
    CanvasDocument Deserialize(string json);

    /// <summary>
    /// Serialize document to stream asynchronously.
    /// </summary>
    Task SerializeAsync(CanvasDocument document, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserialize stream to document asynchronously.
    /// </summary>
    Task<CanvasDocument> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serialize single object.
    /// </summary>
    string SerializeObject(ICanvasObject obj);

    /// <summary>
    /// Deserialize single object.
    /// </summary>
    ICanvasObject DeserializeObject(string json);
}
