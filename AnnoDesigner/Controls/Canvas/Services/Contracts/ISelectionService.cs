using System.Collections.Generic;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Models;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal interface ISelectionService
    {
        HashSet<LayoutObject> SelectedObjects { get; }
        IEnumerable<LayoutObject> SelectedItems { get; }
        void ClearSelection();
    }
}
