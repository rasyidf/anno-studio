using System.Collections.Generic;
using System.Linq;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Services;

/// <summary>
/// Implementation of transform registry service.
/// </summary>
public class TransformRegistry : ITransformRegistry
{
    private readonly Dictionary<string, ITransformOperation> _transforms = new();

    public void RegisterTransform<T>() where T : ITransformOperation, new()
    {
        var transform = new T();
        RegisterTransform(transform);
    }

    public void RegisterTransform(ITransformOperation transform)
    {
        if (string.IsNullOrWhiteSpace(transform.Name))
        {
            throw new System.ArgumentException("Transform name cannot be empty.", nameof(transform));
        }

        _transforms[transform.Name] = transform;
    }

    public void UnregisterTransform(string transformName)
    {
        _transforms.Remove(transformName);
    }

    public ITransformOperation? GetTransform(string name)
    {
        return _transforms.TryGetValue(name, out var transform) ? transform : null;
    }

    public void Execute(string transformName, IEnumerable<ICanvasObject> objects, TransformParameters parameters)
    {
        var transform = GetTransform(transformName);
        if (transform == null)
        {
            throw new System.ArgumentException($"Transform '{transformName}' is not registered.", nameof(transformName));
        }

        var objectsList = objects.ToList();
        if (!transform.CanExecute(objectsList))
        {
            return;
        }

        transform.Execute(objectsList, parameters);
    }

    public IEnumerable<ITransformOperation> GetAllTransforms()
    {
        return _transforms.Values.ToList();
    }
}
