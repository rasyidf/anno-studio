using System;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Base;

/// <summary>
/// Base class for rendering layers.
/// </summary>
public abstract class LayerBase : ILayer
{
    private bool _isVisible = true;
    private float _opacity = 1.0f;
    private bool _isDirty = true;

    public abstract string Name { get; }
    public abstract int ZIndex { get; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                Invalidate();
            }
        }
    }

    public float Opacity
    {
        get => _opacity;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_opacity - clamped) > 0.001f)
            {
                _opacity = clamped;
                Invalidate();
            }
        }
    }

    public virtual SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;

    public bool IsDirty => _isDirty;

    protected ICanvasContext? Context { get; private set; }

    public abstract void Render(SKCanvas canvas, ICanvasContext context);

    public virtual void Update(TimeSpan deltaTime)
    {
    }

    public void Invalidate()
    {
        _isDirty = true;
    }

    public virtual void OnAttached(ICanvasContext context)
    {
        Context = context;
        Invalidate();
    }

    public virtual void OnDetached()
    {
        Context = null;
    }

    protected void ClearDirtyFlag()
    {
        _isDirty = false;
    }
}
