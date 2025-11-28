using System.Collections.Generic;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Manages transform operation registration.
/// </summary>
public interface ITransformRegistry
{
    /// <summary>
    /// Register a transform type.
    /// </summary>
    void RegisterTransform<T>() where T : ITransformOperation, new();

    /// <summary>
    /// Register a transform instance.
    /// </summary>
    void RegisterTransform(ITransformOperation transform);

    /// <summary>
    /// Unregister a transform.
    /// </summary>
    void UnregisterTransform(string transformName);

    /// <summary>
    /// Get transform by name.
    /// </summary>
    ITransformOperation? GetTransform(string name);

    /// <summary>
    /// Execute a registered transform.
    /// </summary>
    void Execute(string transformName, IEnumerable<ICanvasObject> objects, TransformParameters parameters);

    /// <summary>
    /// Get all registered transforms.
    /// </summary>
    IEnumerable<ITransformOperation> GetAllTransforms();
}
