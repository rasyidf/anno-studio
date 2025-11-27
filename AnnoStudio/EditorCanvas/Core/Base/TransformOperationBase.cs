using System;
using System.Collections.Generic;
using System.Linq;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Core.Base;

/// <summary>
/// Base class for transform operations with undo/redo support.
/// </summary>
public abstract class TransformOperationBase : ITransformOperation
{
    protected List<(ICanvasObject obj, Transform2D oldTransform)> PreviousStates { get; private set; } = new();

    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    public virtual string Icon => string.Empty;

    public event EventHandler? CanExecuteChanged;

    public abstract bool CanExecute(IEnumerable<ICanvasObject> objects);

    public abstract void Execute(IEnumerable<ICanvasObject> objects, TransformParameters parameters);

    public virtual void Undo()
    {
        foreach (var (obj, oldTransform) in PreviousStates)
        {
            obj.Transform = oldTransform;
        }
    }

    public virtual void Redo()
    {
        // Re-execute would require storing the parameters
        // For now, derived classes should override if needed
    }

    bool System.Windows.Input.ICommand.CanExecute(object? parameter)
    {
        if (parameter is IEnumerable<ICanvasObject> objects)
        {
            return CanExecute(objects);
        }
        return false;
    }

    void System.Windows.Input.ICommand.Execute(object? parameter)
    {
        if (parameter is (IEnumerable<ICanvasObject> objects, TransformParameters parameters))
        {
            Execute(objects, parameters);
        }
    }

    protected void SavePreviousStates(IEnumerable<ICanvasObject> objects)
    {
        PreviousStates.Clear();
        foreach (var obj in objects)
        {
            PreviousStates.Add((obj, obj.Transform));
        }
    }

    protected void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
