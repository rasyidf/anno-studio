using System.Collections.Generic;

namespace AnnoDesigner.Controls.EditorCanvas.Core
{
    /// <summary>
    /// Optional extended renderer interface that supports registering discrete layers.
    /// </summary>
    public interface ILayeredRenderer : IRenderer
    {
        void AddLayer(IRenderLayer layer);
        bool RemoveLayer(IRenderLayer layer);
        IEnumerable<IRenderLayer> Layers { get; }
    }
}
