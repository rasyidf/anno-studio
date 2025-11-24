using System.Collections.Generic;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal interface ILayoutModelService
    {
        IReadOnlyList<LayoutObject> Items { get; }
        void AddItem(LayoutObject item);
        void RemoveItem(LayoutObject item);
    }
}
