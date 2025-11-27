using System.Text.Json;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Marker interface for objects that can be serialized.
/// </summary>
public interface ISerializable
{
    /// <summary>
    /// Serialize object to JSON element.
    /// </summary>
    JsonElement Serialize();

    /// <summary>
    /// Deserialize object from JSON element.
    /// </summary>
    void Deserialize(JsonElement element);
}
