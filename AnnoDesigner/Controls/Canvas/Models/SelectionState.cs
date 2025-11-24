using System.Collections.Generic;

namespace AnnoDesigner.Controls.Canvas.Models
{
    internal class SelectionState
    {
        public HashSet<CanvasItem> Selected { get; } = new();
    }
}
