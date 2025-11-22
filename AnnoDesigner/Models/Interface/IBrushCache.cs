using System.Windows.Media;

namespace AnnoDesigner.Models.Interface
{
    public interface IBrushCache
    {
        SolidColorBrush GetSolidBrush(Color color);
    }
}
