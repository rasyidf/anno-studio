using System.Collections.Generic;
using System.Windows.Input;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Interfaces;

/// <summary>
/// Represents a transformation operation that can be applied to canvas objects.
/// </summary>
public interface ITransformOperation : ICommand
{
    /// <summary>
    /// Operation identifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Display name for UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Icon for UI.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Check if operation can execute on given objects.
    /// </summary>
    bool CanExecute(IEnumerable<ICanvasObject> objects);

    /// <summary>
    /// Execute transformation.
    /// </summary>
    void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters);

    /// <summary>
    /// Undo transformation.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redo transformation.
    /// </summary>
    void Redo();
}
