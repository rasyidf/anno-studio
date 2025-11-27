using System.Linq;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Rendering.Layers;

/// <summary>
/// Renders canvas objects.
/// </summary>
public class ObjectLayer : LayerBase
{
    public override string Name => "Objects";
    public override int ZIndex => 0;

    public override void Render(SKCanvas canvas, ICanvasContext context)
    {
        if (!IsVisible)
            return;

        // Get visible objects (optionally with culling)
        var objects = context.Settings.Render.EnableCulling
            ? context.Objects.Where(o => o.IsVisible)
            : context.Objects.Where(o => o.IsVisible);

        // Sort by Z-order
        var sortedObjects = objects.OrderBy(o => o.ZOrder);

        // Render each object
        foreach (var obj in sortedObjects)
        {
            obj.Render(canvas, new RenderContext
            {
                Viewport = context.Viewport,
                Settings = context.Settings.Render,
                GridSize = context.Grid.Settings.GridSize,
                Selection = context.Selection
            });
        }

        ClearDirtyFlag();
    }
}
