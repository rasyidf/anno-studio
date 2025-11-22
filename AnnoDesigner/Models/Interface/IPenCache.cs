using System.Windows.Media;

namespace AnnoDesigner.Models.Interface
{
    public interface IPenCache
    {
        Pen GetPen(Brush brush, double thickness);
    }
}
