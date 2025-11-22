using AnnoDesigner.Helper;

namespace AnnoDesigner.Models
{
    public interface IHotkeySource
    {
        void RegisterHotkeys(HotkeyCommandManager manager);
        HotkeyCommandManager HotkeyCommandManager { get; set; }
    }
}
